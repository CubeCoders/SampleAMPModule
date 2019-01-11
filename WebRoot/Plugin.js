//AMP Rust Module - See LICENCE.txt
//©2017-2019 CubeCoders Limited - All rights reserved.

/* eslint eqeqeq: "off", curly: "error", "no-extra-parens": "off" */
/* global API,UI,PluginHandler */

this.plugin = {
    PreInit: function () {
        //Called prior to the plugins initialisation, before the tabs are loaded.
        //This method must not invoke any module/plugin specific API calls.
    },

    PostInit: function () {
        //The tabs have been loaded. You should wire up any event handlers here.

        UI.SetCustomConsoleMessageProcesssor(handleRconTables);
    },

    Reset: function () {

    }
};

this.tabs = [

];

this.stylesheet = "";    //Styles for tab-specific styles

//Your modules private code goes here.
var tableLineReg = /^([\w\/]+?)\s*: (.+?)$/,
    tablePartSepReg = /\s+/,
    extractQuotesReg = /^(.+?)\s\"(.+)\"\s(.+)$/;

function processMatch (match, p1, p2, p3) {
    return (p1 + " \"" + p2.replace(" ", " ") + "\" " + p3); //Replace space with non-breaking (ALT+0160)
}

function handleRconTables(element) {
    var text = element.text();
    var lines = text.split("\n");
    if (tableLineReg.test(lines[0]) === false || lines.length === 1) {
        return false;
    }

    element.text("");

    var newContents = $("<table/>", { "class": "TwoColLine" });
    newContents.append("<thead><tr><th>Key</th><th>Value</th></thead>");
    var newBody = $("<tbody/>");
    var infoTable = true;
    var playerInfoTable = false;
    var parts = null;

    var hashTable, hashBody, hashHead, hashRow;

    for (var line of lines) {
        if (infoTable) {
            parts = line.match(tableLineReg);
            if (parts === null) {
                infoTable = false;
                newContents.append(newBody);
                element.append(newContents);

                if (line[0] !== "#") {
                    element.append($("<pre/>", { text: line }));
                    continue;
                }
            } else {
                var newRow = $("<tr/>");
                newRow.append($("<td/>", { text: parts[1] }));
                newRow.append($("<td/>", { text: parts[2] }));
                newBody.append(newRow);
            }
        }

        if (line.indexOf("id") === 0 || playerInfoTable === true) {
            line = line.replace(extractQuotesReg, processMatch);

            parts = line.split(tablePartSepReg);

            if (parts.length < 5) { continue; }

            if (playerInfoTable === false) {
                hashTable = $("<table/>");
                hashHead = $("<thead/>");
                hashRow = $("<tr/>");

                for (var p = 0; p < parts.length; p++) {
                    hashRow.append($("<th/>", { text: parts[p] }));
                }

                hashHead.append(hashRow);
                hashTable.append(hashHead);
                hashBody = $("<tbody/>");
                hashTable.append(hashBody);

                playerInfoTable = true;
            }
            else {
                hashRow = $("<tr/>");

                for (var q = 0; q < parts.length; q++) {
                    hashRow.append($("<td/>", { text: parts[q] }));
                }

                hashBody.append(hashRow);
            }
        }
        else if (infoTable === false) {
            element.append($("<pre/>", { text: line }));
        }
    }

    if (playerInfoTable) {
        playerInfoTable = false;
        hashTable.append(hashBody);
        element.append(hashTable);
    }

    return false;
}
