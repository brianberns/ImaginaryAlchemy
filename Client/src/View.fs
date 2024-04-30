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

    let private renderConceptOpt
        conceptOpt
        (conceptMap : Map<_, _>)
        visible =
        match conceptOpt with
            | Some concept ->
                renderConceptCard
                    concept
                    conceptMap[concept]
                    ignore
            | None ->
                Html.div [
                    prop.classes [
                        "empty-concept"
                        if not visible then "invisible"
                    ]
                    prop.innerHtml "&nbsp;"
                ]

    let private renderWorkspace model dispatch =
        Html.div [
            prop.id "workspace"
            prop.children [

                Html.div [
                    prop.id "combined-concept"
                    prop.children [
                        renderConceptOpt
                            model.CombinedOpt
                            model.ConceptMap
                            false
                    ]
                ]

                Html.div [
                    prop.id "left-concept"
                    prop.children [
                        renderConceptOpt
                            model.FirstOpt
                            model.ConceptMap
                            true
                    ]
                ]

                Html.button [
                    prop.id "combine"
                    prop.text "Combine"
                    prop.onClick (fun _ ->
                        Combine |> dispatch)
                    prop.disabled
                        (model.IsLoading
                            || model.FirstOpt.IsNone
                            || model.SecondOpt.IsNone)
                ]

                Html.div [
                    prop.id "right-concept"
                    prop.children [
                        renderConceptOpt
                            model.SecondOpt
                            model.ConceptMap
                            true
                    ]
                ]
            ]
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
            if model.IsLoading then
                prop.className "loading"
            prop.children [
                renderWorkspace model dispatch
                renderConceptCards
                    model
                    (Select >> dispatch)
            ]
        ]        
