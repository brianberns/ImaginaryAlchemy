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
        className =
        match conceptOpt with
            | Some concept ->
                renderConceptCard
                    concept
                    conceptMap[concept]
                    ignore
            | None ->
                Html.div [
                    prop.className (className : string)
                    prop.innerHtml "&nbsp;"
                ]

    let private renderWorkspace model dispatch =
        Html.div [
            prop.id "workspace"
            prop.children [

                Html.div [
                    prop.id "workspace-row"
                    prop.children [
                        renderConceptOpt
                            model.CombinedOpt
                            model.ConceptMap
                            "empty-concept-invisible"
                    ]
                ]

                Html.div [
                    prop.id "workspace-row"
                    prop.children [
                        renderConceptOpt
                            model.FirstOpt
                            model.ConceptMap
                            "empty-concept"
                        Html.button [
                            prop.text "Combine"
                            prop.onClick (fun _ ->
                                Combine |> dispatch)
                            prop.disabled
                                (model.FirstOpt.IsNone
                                    || model.SecondOpt.IsNone)
                        ]
                        renderConceptOpt
                            model.SecondOpt
                            model.ConceptMap
                            "empty-concept"
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
