#load "utils/utils.fsx"
#load "utils/config.fsx"

open MBrace.Azure
open MBrace.Azure.Management

// This script is used to reconnect to your cluster.

// You can download your publication settings file at 
//     https://manage.windowsazure.com/publishsettings
let pubSettingsFile : string = (!?)

// If your publication settings defines more than one subscription,
// you will need to specify which one you will be using here.
let subscriptionId : string option = (!?)

// Your prefered Azure service name for the cluster.
// NB: must be a valid DNS prefix unique across Azure.
let clusterName : string = (!?)

// Your prefered Azure region. Assign this to a data center close to your location.
let region : Region = (!?)
// Your prefered VM size
let vmSize : VMSize = (!?)
// Your prefered cluster count
let vmCount : int = (!?)

// Update your config file with current cloud settings
Config.UpdateConfig(pubSettingsFile, subscriptionId, clusterName, region, vmSize, vmCount)