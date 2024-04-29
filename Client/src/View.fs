namespace ImaginaryAlchemy

open Feliz

module View =

    let private renderConceptCard
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

    let private renderInput
        conceptOpt
        (conceptMap : Map<_, _>) =
        match conceptOpt with
            | Some concept ->
                renderConceptCard
                    concept
                    conceptMap[concept]
                    ignore
            | None ->
                Html.div [
                    prop.className "empty-input"
                ]

    let private renderSelected model dispatch =
        Html.div [
            renderInput
                model.FirstOpt
                model.ConceptMap
            Html.button [
                prop.text "Combine"
                prop.onClick (fun _ ->
                    Combine |> dispatch)
                prop.disabled
                    (model.FirstOpt.IsNone
                        || model.SecondOpt.IsNone)
            ]
            renderInput
                model.SecondOpt
                model.ConceptMap
        ]

    let private renderConceptCards model dispatch =
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
