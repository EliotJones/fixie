namespace FSharp.TestLibrary

open Fixie.FSharp

type SampleFSharpConvention() =
    inherit FSharpConvention()
    do
        // base.Methods.Where (fun m -> m.IsStatic) |> ignore
        base.Classes.Where (fun t -> t.Name.EndsWith("Tests")) |> ignore