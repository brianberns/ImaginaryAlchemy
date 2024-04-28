namespace ImaginaryAlchemy

type Concept = string

type ConceptInfo =
    {
        Concept : Concept
        Generation : int
    }

type IAlchemyApi =
    {
        Combine :
            (Concept * Concept) ->
                Async<Option<ConceptInfo>>
    }
