namespace ImaginaryAlchemy

open Feliz

module View =

    let renderConceptCard
        concept
        gen
        dispatch =
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
                        (String.replicate gen "&bull;")
                ]
            ]
            prop.onClick (fun _ ->
                dispatch concept)
        ]

    let renderSelected model dispatch =
        let concepts =
            [ model.FirstOpt; model.SecondOpt ]
                |> List.choose id
        Html.div [
            for concept in concepts do
                renderConceptCard
                    concept
                    model.ConceptMap[concept]
                    ignore
            Html.button [
                prop.text "Combine"
                prop.onClick (fun _ ->
                    Combine |> dispatch)
                prop.disabled (concepts.Length <> 2)
            ]
        ]

    let renderConceptCards model dispatch =
        Html.div [
            prop.className "concept-cards"
            model.ConceptMap
                |> Map.toSeq
                |> Seq.sortBy (fun (concept, gen) ->
                    gen, concept)
                |> Seq.map (fun (concept, gen) ->
                    renderConceptCard
                        concept
                        gen
                        dispatch)
                |> prop.children
        ]

    let render model dispatch =
        Html.div [
            renderSelected model dispatch
            renderConceptCards
                model
                (Select >> dispatch)
        ]        
