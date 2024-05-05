namespace ImaginaryAlchemy

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

type Concept = string

type IAlchemyApi =
    {
        Combine :
            (Concept * Concept) ->
                Async<Result<Concept * (*isNew*) bool, string>>
    }
