#load "utils.fsx"
#load "../../../packages/MBrace.Azure/MBrace.Azure.fsx"
#load "../../../packages/MBrace.Azure.Management/MBrace.Azure.Management.fsx"

open System.IO
open System.Threading
open System.Net
open System.Net.Mail
open MBrace.Core
open MBrace.Runtime
open MBrace.Azure
open MBrace.Azure.Management

[<AutoSerializable(false)>]
type CredentialMailDistributor private (sender : string, smtp : ThreadLocal<SmtpClient>) =

    let send recipient title body =
        let client = smtp.Value
        let mail = new MailMessage()
        mail.From <- new MailAddress(sender)
        mail.To.Add(recipient : string)
        mail.Subject <- title
        mail.IsBodyHtml <- false
        mail.Body <- body
        client.Send mail

    do send sender "Credential mail distributor notification" "Test."

    member __.SendCredentials(recipient : string, deployment : Deployment) =
        let title = "Your mbrace credentials for Progressive F# Tutorials 2015"
        let body = 
            sprintf """
Hi %s,

Here are the credentials for your own mbrace cluster '%s'.
Please copy the following F# snippet to 'utils/config.fsx' found in your tutorial solution:

let storageConnectionString = "%s"
let serviceBusConnectionString = "%s"
let configuration = new Configuration(storageConnectionString, serviceBusConnectionString) |> Some

Cheers!
"""             
                recipient 
                deployment.ServiceName 
                deployment.Configuration.StorageConnectionString 
                deployment.Configuration.ServiceBusConnectionString

        send recipient title body

    static member Create(sender : string, password : string, ?smtpServer : string, ?username : string) =
        let username = defaultArg username sender
        let smtpServer =
            match smtpServer with
            | Some s -> s
            | None when sender.EndsWith "@gmail.com" -> "smtp.gmail.com"
            | None when sender.EndsWith "@hotmail.com" -> "smtp.live.com"
            | None -> invalidArg "smtpServer" "must supply an smtp server"

        let mkClient () =
            let client = new SmtpClient(smtpServer)
            client.Port <- 587
            client.UseDefaultCredentials <- false
            client.Credentials <- new NetworkCredential(username, password)
            client.EnableSsl <- true
            client

        new CredentialMailDistributor(sender, new ThreadLocal<_>(mkClient))


// You can download your publication settings file at 
//     https://manage.windowsazure.com/publishsettings
let pubSettingsFile : string = (!?)

// If your publication settings defines more than one subscription,
// you will need to specify which one you will be using here.
let subscriptionId : string option = (!?)

// administrator email
let adminEmail : string = (!?)

// administrator password
let adminPasswd : string = (!?)

let clusterSize = 6
let vmSize = VMSize.A2
let region = Region.North_Europe

/// subscription manager
let manager = SubscriptionManager.FromPublishSettingsFile(pubSettingsFile, defaultRegion = region, logger = new ConsoleLogger(), ?subscriptionId = subscriptionId)
/// credential mail distributor
let credMail = CredentialMailDistributor.Create(adminEmail, adminPasswd)

/// provision cluster for supplied email address
let provisionForEmailAddressAsync(emailAddress : string) =
    let d = manager.Provision(clusterSize, vmSize = vmSize, serviceLabel = emailAddress, reuseAccounts = false)
    Async.StartAsTask(async { let! _ = d.AwaitProvisionAsync() in return (credMail.SendCredentials(emailAddress, d); d) })