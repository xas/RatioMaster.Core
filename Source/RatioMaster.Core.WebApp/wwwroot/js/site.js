// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
var connection = new signalR.HubConnectionBuilder().withUrl("/ratioHub").configureLogging(signalR.LogLevel.Debug).build();

connection.on("UpdateMetrics", function (metricsModel) {
    document.getElementById("metrics-upload").value = metricsModel.uploaded;
    document.getElementById("metrics-download").value = metricsModel.downloaded;
    document.getElementById("metrics-seeders").value = metricsModel.seeders;
    document.getElementById("metrics-leechers").value = metricsModel.leechers;
    document.getElementById("metrics-time").value = metricsModel.totalTime;
});

connection.on("SendTorrentInfo", function (infoModel) {
    document.getElementById("info-file").value = infoModel.fileName;
    document.getElementById("info-hash").value = infoModel.hash;
    document.getElementById("info-tracker").value = infoModel.tracker;
    document.getElementById("info-peers").value = infoModel.peers;
    document.getElementById("info-peerId").value = infoModel.peerId;
    document.getElementById("info-client").value = infoModel.clientKey;
    document.getElementById("info-port").value = infoModel.port;
});

connection.start().then(function () {
    console.log('ok');
}).catch(function (err) {
    return console.error(err.toString());
});
