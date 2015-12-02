#load "utils/utils.fsx"
#load "utils/config.fsx"

open MBrace.Azure
open MBrace.Azure.Management

// This script is used to reconnect to your cluster.

// You can download your publication settings file at 
//     https://manage.windowsazure.com/publishsettings
let pubSettingsFile : string = __add_me__

// If your publication settings defines more than one subscription,
// you will need to specify which one you will be using here.
let subscriptionId : string option = __add_me__

// Your prefered Azure service name for the cluster.
// NB: must be a valid DNS prefix unique across Azure.
let clusterName : string = __add_me__

// Your prefered Azure region. Assign this to a data center close to your location.
let region : Region = __add_me__
// Your prefered VM size
let vmSize : VMSize = __add_me__
// Your prefered cluster count
let vmCount = __add_me__

// Update your config file with current cloud settings
Config.UpdateConfig(pubSettingsFile, subscriptionId, clusterName, region, vmSize, vmCount)