// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

/// <reference group="Generic" /> 
/// <reference path="C:\Program Files (x86)\Microsoft Visual Studio 12.0\JavaScript\References\DocDbWrapperScript.js" /> 
/// http://dl.windowsazure.com/documentDB/jsserverdocs/

function getUserByLogin_v1(loginid) {
    var collection = getContext().getCollection();
    var query = 'SELECT VALUE r FROM root r JOIN l IN r.Logins WHERE l.id = "' + loginid + '"';
    //Get the Userid by LoginId
    var isEmailAccepted = collection.queryDocuments(
        collection.getSelfLink(),
        query,
        function (err, feed, options) {
            if (err) throw err;
            getContext().getResponse().setBody(feed);
        });

    }