namespace ImaginaryAlchemy

open System.Collections.Generic

module Prelude =

    let memoize f =
        let cache = Dictionary<_, _>()
        fun key ->
            match cache.TryGetValue(key) with
                | true, value -> value
                | _ -> 
                    let value = f key
                    cache.Add(key, value)
                    value

/// Option computation expression builder.
type OptionBuilder() =
    member _.Bind(opt, f) = Option.bind f opt
    member _.Return(x) = Some x
    member _.ReturnFrom(opt : Option<_>) = opt
    member _.Zero() = None

[<AutoOpen>]
module OptionBuilder =

    /// Option computation expression builder.
    let option = OptionBuilder()
