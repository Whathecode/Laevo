using Whathecode.System.Aspects;
using PostSharp.Patterns.Diagnostics;


[assembly: InitializeEventHandlers( AttributeTargetTypes = "Laevo.*" )]
[assembly: LogException( AttributeTargetTypes = "Laevo.*" )]

namespace Laevo { }
