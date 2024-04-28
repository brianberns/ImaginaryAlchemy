namespace ImaginaryAlchemy

open Browser.Dom
open Fable.Remoting.Client

module App =

    let alchemyApi =
        Remoting.createApi()
            |> Remoting.buildProxy<IAlchemyApi>

    // Get a reference to our button and cast the Element to an HTMLButtonElement
    let myButton = document.querySelector(".my-button") :?> Browser.Types.HTMLButtonElement
    let myList = document.querySelector(".my-list") :?> Browser.Types.HTMLUListElement

    // Register our listener
    myButton.onclick <- fun _ ->
        async {
            let! conceptOpt = alchemyApi.Combine("Red", "Blue")
            let item =
                document.createElement("li")
                    |> myList.appendChild
            document.createTextNode($"{conceptOpt}")
                |> item.appendChild
                |> ignore
        } |> Async.StartImmediate
