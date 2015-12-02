#load "config.fsx"

open MBrace.Azure
open MBrace.Azure.Management

// This script is used to reconnect to your cluster.

// You can download your publication settings file at 
//     https://manage.windowsazure.com/publishsettings
let pubSettingsFile = @"C:\path\to\your.publishsettings"

// If your publication settings defines more than one subscription,
// you will need to specify which one you will be using here.
let subscriptionId : string option = None

// Your prefered Azure service name for the cluster.
// NB: must be a valid DNS prefix unique across Azure.
let clusterName = "enter a valid cloud service name"

// Your prefered Azure region. Assign this to a data center close to your location.
let region = Region.North_Europe
// Your prefered VM size
let vmSize = VMSize.Large
// Your prefered cluster count
let vmCount = 4

// Update your config file with current cloud settings
Config.UpdateConfig(pubSettingsFile, subscriptionId, clusterName, region, vmSize, vmCount)