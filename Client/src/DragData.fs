namespace ImaginaryAlchemy

open Browser.Types

/// Data carried during an HTML drag/drop operation.
type private DragData =
    {
        /// Concept being dragged.
        Concept : Concept
    }

module DragData =

    // https://stackoverflow.com/questions/40940288/drag-datatransfer-data-unavailable-in-ondragover-event
    // https://stackoverflow.com/questions/31915653/how-to-get-data-from-datatransfer-getdata-in-event-dragover-or-dragenter
    let mutable private shared : Option<DragData> = None

    /// Sets drag data for the given event.
    let private setData dragData (evt : DragEvent) =
        evt.dataTransfer.setData(   // must call for iOS?
            "text/plain",
            dragData.Concept)
            |> ignore
        shared <- Some dragData

    /// Gets drag data for the given event.
    let private getData (evt : DragEvent) =
        shared

    /// Sets the concept being dragged.
    let setConcept concept evt =
        setData { Concept = concept } evt

    /// Gets the concept being dragged.
    let getConcept evt =
        match getData evt with
            | Some dragData -> dragData.Concept
            | None -> failwith "Unexpected"
