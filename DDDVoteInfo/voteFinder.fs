namespace DDDVoteInfo

open Microsoft.WindowsAzure.Storage
open FSharp.Azure.Storage.Table
open System

type Vote =
    { [<PartitionKey>] Year: string
      [<RowKey>] Id: string
      SessionId: Guid
      TicketNumber: string }

module voteFinder =
    let getVotes year connStr =
        let account = CloudStorageAccount.Parse connStr
        let tableClient = account.CreateCloudTableClient()

        Query.all<Vote>
        |> Query.where <@ fun _ s -> s.PartitionKey = year @>
        |> fromTableAsync tableClient "Votes"