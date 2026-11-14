// Replace these values with an assembly already loaded in dnSpy.
var filePath = @"C:\path\to\your\assembly.exe";
var moduleName = "YourAssembly.exe";
var typeName = "YourNamespace.YourType";
var methodSignature = "System.Void YourNamespace.YourType::YourMethod()";

const int typeLimit = 5000;
var matchedTypes = ctx.Documents.ListTypes(filePath, typeLimit)
    .Where(type =>
        type.ModuleName == moduleName &&
        type.FullName == typeName)
    .ToArray();

if (matchedTypes.Length != 1) {
    return $"在前 {typeLimit} 个类型中，匹配到 "
        + $"{matchedTypes.Length} 个目标类型：{typeName}。";
}

var targetType = matchedTypes[0];
var matchedMethods = ctx.Documents.ListMethods(
        filePath,
        moduleName,
        targetType.Token,
        limit: Math.Max(1, targetType.MethodCount))
    .Where(method => method.FullName == methodSignature)
    .ToArray();

if (matchedMethods.Length != 1) {
    return $"在目标类型中匹配到 {matchedMethods.Length} 个方法："
        + methodSignature;
}

var targetMethod = matchedMethods[0];

var code = ctx.Decompiler.DecompileMethod(
    filePath,
    moduleName,
    targetMethod.Token);

return $"路径：{filePath}"
    + Environment.NewLine
    + $"模块：{moduleName}"
    + Environment.NewLine
    + $"类型：{targetType.FullName}"
    + Environment.NewLine
    + $"方法：0x{targetMethod.Token:X8}  {targetMethod.FullName}"
    + Environment.NewLine
    + Environment.NewLine
    + code;
