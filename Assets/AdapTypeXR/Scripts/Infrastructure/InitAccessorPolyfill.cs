// Unity targets .NET Standard 2.1 which does not include the IsExternalInit
// sentinel type required by C# 9 `init` property accessors. Defining it here
// in the correct namespace makes the compiler recognise `init` project-wide.
// This file is safe to keep in any Unity 2021+ / Unity 6 project.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
