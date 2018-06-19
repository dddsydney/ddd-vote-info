module Program

open System
open System.IO
open Microsoft.Extensions.Configuration
open DDDVoteInfo
open DDDVoteInfo.voteFinder
open DDDVoteInfo.sessionFinder

let sessionIdMap = fun (s: Session, _) -> s.RowKey
let voteExists sessions (vote: Vote) =
    sessions
    |> Seq.map sessionIdMap
    |> Seq.exists (fun id -> vote.SessionId.ToString() = id)

type SessionVote =
     { Title: string
       Presenter: string
       TrackLength: string
       SessionId: string
       TicketVote: bool }

type VoteResult =
     { Title: string
       Presenter: string
       TrackLength: string
       TotalVotes: int
       TicketHolderVotes: int
       NonTicketHolderVotes: int }

let countVotes votes sessions =
  votes
  |> Seq.filter(fun (v, _) -> v |> voteExists sessions)
  |> Seq.map(fun (v, _) -> 
            let (session, _) = sessions |> Seq.find(fun (s, _) -> s.RowKey = v.SessionId.ToString())
            { Title = session.SessionTitle
              Presenter = session.PresenterName
              TrackLength = session.SessionLength
              SessionId = session.RowKey
              TicketVote = v.TicketNumber = "" })
  |> Seq.groupBy(fun r -> r.Title)
  |> Seq.map(fun (key, sess) ->
             let ticketVotes = sess |> Seq.filter(fun s -> s.TicketVote) |> Seq.length
             let nonTicketVotes = sess |> Seq.filter(fun s -> not s.TicketVote) |> Seq.length
             let voteInfo = sess |> Seq.head // the rest of the info we can get from just the first session
             { Title = key
               Presenter = voteInfo.Presenter
               TrackLength = voteInfo.TrackLength
               TotalVotes = (ticketVotes * 2) + nonTicketVotes
               TicketHolderVotes = ticketVotes
               NonTicketHolderVotes = nonTicketVotes })
   |> Seq.sortByDescending(fun r -> r.TotalVotes)

let printVotes category count countedVotes =
  let titleLength = countedVotes |> Seq.map(fun v -> v.Title.Length) |> Seq.sortByDescending id |> Seq.head
  let presenterLength = countedVotes |> Seq.map(fun v -> v.Presenter.Length) |> Seq.sortByDescending id |> Seq.head

  printfn "Top %d %s sessions" count category
  printfn "Total | THV | NTHV | %-*s | %-*s | Session Length" titleLength "Title" presenterLength "Presenter"
  countedVotes
  |> Seq.take count
  |> Seq.iter (fun v -> printfn "%5d | %3d | %4d | %-*s | %-*s | %s" v.TotalVotes v.TicketHolderVotes v.NonTicketHolderVotes titleLength v.Title presenterLength v.Presenter v.TrackLength)
  printfn "----"
  printfn ""

[<EntryPoint>]
let main argv =
    let config = (new ConfigurationBuilder())
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json", true, true)
                  .Build()

    async {
        let! votes = getVotes "2018" (config.GetConnectionString("AzureStorage"))
        let! sessions = getSessions "2018" (config.GetConnectionString("AzureStorage"))

        sessions
        |> Seq.filter(fun (s, _) -> s.TrackType = "Developer")
        |> countVotes votes
        |> printVotes "dev" 10

        sessions
        |> Seq.filter(fun (s, _) -> s.TrackType = "Data" || s.TrackType = "Design")
        |> countVotes votes
        |> printVotes "data & design" 5

        sessions
        |> Seq.filter(fun (s, _) -> s.TrackType = "Junior Dev")
        |> countVotes votes
        |> printVotes "junior dev" 5

        return ignore
    } |> Async.RunSynchronously
      |> ignore

    printfn "We are done"

    0 // return an integer exit code