using PostSharp.Patterns.Diagnostics;


[assembly: LogException( AttributeTargetTypes = "Laevo.*" )]

// ReSharper disable once CheckNamespace
namespace Laevo { }
