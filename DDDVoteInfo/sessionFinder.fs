namespace DDDVoteInfo

open Microsoft.WindowsAzure.Storage
open FSharp.Azure.Storage.Table

type Session =
              { [<PartitionKey>] Year: string
                [<RowKey>] RowKey: string
                SessionTitle: string
                SessionLength: string
                TrackType: string
                PresenterName: string }

module sessionFinder =
    let getSessions year connStr =
        let account = CloudStorageAccount.Parse connStr
        let tableClient = account.CreateCloudTableClient()

        Query.all<Session>
        |> Query.where <@ fun _ s -> s.PartitionKey = (sprintf "Session-%s" year) @>
        |> fromTableAsync tableClient "Sessions"