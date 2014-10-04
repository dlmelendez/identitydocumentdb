// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

/// <reference group="Generic" /> 
/// <reference path="C:\Program Files (x86)\Microsoft Visual Studio 12.0\JavaScript\References\DocDbWrapperScript.js" /> 
/// http://dl.windowsazure.com/documentDB/jsserverdocs/

function getUserById_v1(userid) {
    var collection = getContext().getCollection();
    //Query documents by UserId 
    var isAccepted = collection.queryDocuments(
        collection.getSelfLink(),
        'SELECT * FROM root r WHERE r.UserId = "' + userid + '"',
        function (err, feed, options) {
            if (err) throw err;
            getContext().getResponse().setBody(feed);
        });

}

