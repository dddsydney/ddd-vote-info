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
       SessionId: string
       TicketVote: bool }

type VoteResult =
     { Title: string
       TotalVotes: int
       TicketHolderVotes: int
       NonTicketHolderVotes: int }

let countVotes votes sessions =
  votes
  |> Seq.filter(fun (v, _) -> v |> voteExists sessions)
  |> Seq.map(fun (v, _) -> 
            let (session, _) = sessions |> Seq.find(fun (s, _) -> s.RowKey = v.SessionId.ToString())
            { Title = session.SessionTitle
              SessionId = session.RowKey
              TicketVote = v.TicketNumber = "" })
  |> Seq.groupBy(fun r -> r.Title)
  |> Seq.map(fun (key, sess) ->
             let ticketVotes = sess |> Seq.filter(fun s -> s.TicketVote) |> Seq.length
             let nonTicketVotes = sess |> Seq.filter(fun s -> not s.TicketVote) |> Seq.length
             { Title = key
               TotalVotes = (ticketVotes * 2) + nonTicketVotes
               TicketHolderVotes = ticketVotes
               NonTicketHolderVotes = nonTicketVotes })
   |> Seq.sortByDescending(fun r -> r.TotalVotes)

let printVotes category count countedVotes =
  printfn "Top %d %s sessions" count category
  printfn "Total\tTHV\tNTHV\tTitle"
  countedVotes
  |> Seq.take count
  |> Seq.iter (fun v -> printfn "%d\t%d\t%d\t%s" v.TotalVotes v.TicketHolderVotes v.NonTicketHolderVotes v.Title)
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