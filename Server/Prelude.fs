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
