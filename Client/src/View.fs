namespace ImaginaryAlchemy

open Feliz

module View =

    let private renderConceptCard
        concept
        gen =
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
            prop.draggable true
            prop.onDragStart (fun evt ->
                // Audio.enable ()
                DragData.setConcept concept evt)
        ]

    /// Renders drag/drop properties.
    let private renderDragDrop allow dispatch =
        [
                // start highlighting the target?
            prop.onDragEnter (fun evt ->
                if allow evt |> Option.isSome then
                    evt.preventDefault()
                    (*dispatch "highlight"*))

                // stop highlighting the target?
            prop.onDragLeave (fun evt ->
                if allow evt |> Option.isSome then
                    evt.preventDefault()
                    (*dispatch "no-highlight"*))

                // allow drop?
            prop.onDragOver (fun evt ->
                if allow evt |> Option.isSome then
                    evt.preventDefault())

                // drop has occurred
            prop.onDrop (fun evt ->
                evt.preventDefault()
                match allow evt with
                    | Some msg -> dispatch msg
                    | None -> ())
        ]

    let private renderConceptSpot
        conceptOpt
        (conceptMap : Map<_, _>)
        makeMsgOpt
        dispatch =
        Html.div [
            match conceptOpt with
                | Some concept ->
                    prop.children [
                        renderConceptCard
                            concept
                            conceptMap[concept]
                    ]
                | None ->
                    prop.className [
                        "empty-concept"
                        if Option.isNone makeMsgOpt then
                            "invisible"
                    ]
                    prop.innerHtml "&nbsp;"
            match makeMsgOpt with
                | Some makeMsg ->
                    yield! renderDragDrop
                        (fun evt ->
                            let concept = DragData.getConcept evt
                            Some (makeMsg concept))
                        dispatch
                | None -> ()
        ]

    let private renderWorkspace model dispatch =
        Html.div [
            prop.id "workspace"
            prop.children [

                Html.div [
                    prop.id "combined-concept"
                    prop.children [
                        renderConceptSpot
                            model.CombinedOpt
                            model.ConceptMap
                            None
                            dispatch
                    ]
                ]

                Html.div [
                    prop.id "left-concept"
                    prop.children [
                        renderConceptSpot
                            model.FirstOpt
                            model.ConceptMap
                            (Some SetFirst)
                            dispatch
                    ]
                ]

                Html.button [
                    prop.id "combine"
                    prop.text "+"
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
                        renderConceptSpot
                            model.SecondOpt
                            model.ConceptMap
                            (Some SetSecond)
                            dispatch
                    ]
                ]
            ]
        ]

    let private renderConceptCards model =
        Html.div [
            prop.className "concept-cards"
            model.ConceptMap
                |> Map.toSeq
                |> Seq.sortBy (fun (concept, gen) ->
                    gen, concept)
                |> Seq.map (fun (concept, gen) ->
                    renderConceptCard
                        concept
                        gen)
                |> prop.children
        ]

    let render model dispatch =
        Html.div [
            if model.IsLoading then
                prop.className "loading"
            prop.children [
                renderWorkspace model dispatch
                renderConceptCards model
            ]
        ]        
