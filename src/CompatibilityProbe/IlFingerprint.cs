using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace CompatibilityProbe;

internal static class IlFingerprint
{
    private static readonly Dictionary<ushort, OpCode> OpCodesByValue = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!).ToDictionary(o => unchecked((ushort)o.Value));

    internal static string TypeSignature(Type type)
    {
        if (type.IsByRef) return TypeSignature(type.GetElementType()!) + "&";
        if (type.IsPointer) return TypeSignature(type.GetElementType()!) + "*";
        if (type.IsArray) return TypeSignature(type.GetElementType()!) + (type.IsSZArray ? "[]" : "[" + new string(',', type.GetArrayRank() - 1) + "]");
        if (type.IsGenericParameter) return (type.DeclaringMethod == null ? "!" : "!!") + type.GenericParameterPosition;
        string name = "[" + type.Assembly.GetName().Name + "]" + (type.IsGenericType ? type.GetGenericTypeDefinition().FullName : type.FullName);
        return type.IsGenericType ? name + "<" + string.Join(",", type.GetGenericArguments().Select(TypeSignature)) + ">" : name;
    }
    internal static string MemberSignature(MemberInfo member) => member switch {
        Type type => TypeSignature(type),
        FieldInfo field => TypeSignature(field.DeclaringType!) + "::" + field.Name + ":" + TypeSignature(field.FieldType),
        MethodBase method => TypeSignature(method.DeclaringType!) + "::" + method.Name
            + (method.IsGenericMethod ? "<" + string.Join(",", method.GetGenericArguments().Select(TypeSignature)) + ">" : "")
            + "(" + string.Join(",", method.GetParameters().Select(ParameterSignature)) + ")"
            + "->" + (method is MethodInfo info ? TypeSignature(info.ReturnType) : "void")
            + "|" + method.CallingConvention,
        _ => throw new NotSupportedException("Unsupported IL member: " + member.MemberType)
    };
    private static string ParameterSignature(ParameterInfo parameter) => TypeSignature(parameter.ParameterType)
        + string.Concat(parameter.GetRequiredCustomModifiers().Select(t => " modreq(" + TypeSignature(t) + ")"))
        + string.Concat(parameter.GetOptionalCustomModifiers().Select(t => " modopt(" + TypeSignature(t) + ")"));

    internal static object Read(MethodBase method)
    {
        var body = method.GetMethodBody();
        if (body == null) return new { kind = "no-managed-body", implementation = method.GetMethodImplementationFlags().ToString() };
        byte[] il = body.GetILAsByteArray()!;
        var raw = new List<(int Offset, string Opcode, object? Operand)>();
        var constants = new Dictionary<string, object>(StringComparer.Ordinal);
        Type[]? typeArgs = method.DeclaringType?.IsGenericType == true ? method.DeclaringType.GetGenericArguments() : null;
        Type[]? methodArgs = method.IsGenericMethod ? method.GetGenericArguments() : null;
        int position = 0;
        int Int32() { int value = BitConverter.ToInt32(il, position); position += 4; return value; }
        long Int64() { long value = BitConverter.ToInt64(il, position); position += 8; return value; }
        while (position < il.Length)
        {
            int offset = position;
            ushort value = il[position++];
            if (value == 0xfe) value = (ushort)(0xfe00 | il[position++]);
            var opcode = OpCodesByValue[value];
            object? operand;
            switch (opcode.OperandType)
            {
                case OperandType.InlineNone: operand = null; break;
                case OperandType.ShortInlineI: operand = (sbyte)il[position++]; break;
                case OperandType.InlineI: operand = Int32(); break;
                case OperandType.InlineI8: operand = Int64(); break;
                // Preserve float bit patterns (NaN payloads and negative zero included).
                case OperandType.ShortInlineR: operand = "float-bits:" + unchecked((uint)Int32()).ToString("x8", CultureInfo.InvariantCulture); break;
                case OperandType.InlineR: operand = "double-bits:" + unchecked((ulong)Int64()).ToString("x16", CultureInfo.InvariantCulture); break;
                case OperandType.ShortInlineVar: operand = (int)il[position++]; break;
                case OperandType.InlineVar: operand = (int)BitConverter.ToUInt16(il, position); position += 2; break;
                case OperandType.ShortInlineBrTarget: { int distance = (sbyte)il[position++]; operand = new Branch(position + distance); break; }
                case OperandType.InlineBrTarget: { int distance = Int32(); operand = new Branch(position + distance); break; }
                case OperandType.InlineSwitch:
                {
                    int count = Int32();
                    var distances = Enumerable.Range(0, count).Select(_ => Int32()).ToArray();
                    operand = distances.Select(d => new Branch(position + d)).ToArray(); break;
                }
                case OperandType.InlineString: operand = new { literal = method.Module.ResolveString(Int32()) }; break;
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                {
                    MemberInfo member = method.Module.ResolveMember(Int32(), typeArgs, methodArgs)!;
                    string signature = MemberSignature(member);
                    operand = new { member = signature };
                    if (member is FieldInfo field && (field.Attributes & FieldAttributes.HasFieldRVA) != 0)
                        constants.TryAdd(signature, RvaData(field));
                    break;
                }
                // A raw calli signature blob embeds metadata indexes. Never claim a stable hash for it.
                case OperandType.InlineSig: throw new NotSupportedException("InlineSig/calli requires signature decoding; fingerprint withheld rather than hashing volatile tokens.");
                default: throw new NotSupportedException("Unsupported IL operand: " + opcode.OperandType);
            }
            raw.Add((offset, opcode.Name!, operand));
        }
        var indices = raw.Select((i, index) => (i.Offset, index)).ToDictionary(p => p.Offset, p => p.index);
        indices.Add(il.Length, raw.Count);
        int Index(int offset) => indices.TryGetValue(offset, out int index) ? index : throw new InvalidDataException("IL branch or EH boundary does not point to an instruction.");
        object? Normalize(object? operand) => operand switch {
            Branch branch => new { instruction = Index(branch.Offset) },
            Branch[] branches => branches.Select(b => Index(b.Offset)).ToArray(),
            _ => operand
        };
        return new {
            kind = "managed-il", body.InitLocals, body.MaxStackSize,
            locals = body.LocalVariables.Select(l => new { type = TypeSignature(l.LocalType), l.IsPinned }).ToArray(),
            instructions = raw.Select(i => new { opcode = i.Opcode, operand = Normalize(i.Operand) }).ToArray(),
            exceptionRegions = body.ExceptionHandlingClauses.Select(c => new {
                flags = c.Flags.ToString(), tryStart = Index(c.TryOffset), tryEnd = Index(c.TryOffset + c.TryLength),
                handlerStart = Index(c.HandlerOffset), handlerEnd = Index(c.HandlerOffset + c.HandlerLength),
                filterStart = c.Flags == ExceptionHandlingClauseOptions.Filter ? (int?)Index(c.FilterOffset) : null,
                catchType = c.Flags == ExceptionHandlingClauseOptions.Clause && c.CatchType != null ? TypeSignature(c.CatchType) : null
            }).ToArray(),
            rvaConstants = constants.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new { signature = p.Key, data = p.Value }).ToArray()
        };
    }
    private sealed record Branch(int Offset);
    private static object RvaData(FieldInfo field)
    {
        using var stream = File.OpenRead(field.Module.FullyQualifiedName);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        var definition = metadata.GetFieldDefinition((FieldDefinitionHandle)MetadataTokens.Handle(field.MetadataToken));
        int rva = definition.GetRelativeVirtualAddress();
        Type fieldType = field.FieldType;
        int size = fieldType == typeof(byte) || fieldType == typeof(sbyte) || fieldType == typeof(bool) ? 1
            : fieldType == typeof(short) || fieldType == typeof(ushort) || fieldType == typeof(char) ? 2
            : fieldType == typeof(int) || fieldType == typeof(uint) || fieldType == typeof(float) ? 4
            : fieldType == typeof(long) || fieldType == typeof(ulong) || fieldType == typeof(double) ? 8 : 0;
        if (size == 0 && fieldType.Module == field.Module)
            size = metadata.GetTypeDefinition((TypeDefinitionHandle)MetadataTokens.Handle(fieldType.MetadataToken)).GetLayout().Size;
        if (rva == 0 || size <= 0) throw new InvalidDataException("Cannot determine RVA data length: " + MemberSignature(field));
        byte[] bytes = pe.GetSectionData(rva).GetContent(0, size).ToArray();
        return new { byteLength = bytes.Length, sha256 = Program.Sha(bytes), base64 = Convert.ToBase64String(bytes) };
    }
}
