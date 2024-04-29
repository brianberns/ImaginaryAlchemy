namespace ImaginaryAlchemy

open Feliz

module View =

    let renderConceptCard concept (conceptMap : Map<_, _>) dispatchOpt =
        Html.div [
            prop.className "concept-card"
            prop.children [
                Html.span [
                    prop.className "concept"
                    prop.text (concept : Concept)
                ]
                Html.span [
                    prop.className "generation"
                    prop.innerHtml
                        (String.replicate
                            conceptMap[concept]
                            "&bull;")
                ]
            ]
            match dispatchOpt with
                | Some dispatch ->
                    prop.onClick (fun _ ->
                        dispatch concept)
                | None -> ()
        ]

    let renderSelected model dispatch =
        Html.div [
            renderConceptCard model.First model.ConceptMap None
            renderConceptCard model.Second model.ConceptMap None
            Html.button [
                prop.text "Combine"
                prop.onClick (fun _ ->
                    Combine |> dispatch)
            ]
        ]

    let renderConceptCards model dispatch =
        Html.div [
            prop.className "concept-cards"
            model.ConceptMap.Keys
                |> Seq.map (fun concept ->
                    renderConceptCard concept model.ConceptMap dispatch)
                |> prop.children
        ]

    let render model dispatch =
        Html.div [
            renderSelected model dispatch
            renderConceptCards
                model
                (Some (SetFirst >> dispatch))
            renderConceptCards
                model
                (Some (SetSecond >> dispatch))
        ]        
