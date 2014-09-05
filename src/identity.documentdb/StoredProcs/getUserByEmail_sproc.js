// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

/// <reference group="Generic" /> 
/// <reference path="C:\Program Files (x86)\Microsoft Visual Studio 12.0\JavaScript\References\DocDbWrapperScript.js" /> 

function getUserByEmail(email) {
    var collection = getContext().getCollection();

    var emailQuery = 'SELECT r.UserId FROM root r WHERE r.Email = "' + email + '"';
    var userId = '';
    //Get the Userid by email
    var isEmailAccepted = collection.queryDocuments(
        collection.getSelfLink(),
        emailQuery,
        function (err, feed, options) {
            if (err) throw err;

            if (!feed || !feed.length) getContext().getResponse().setBody(feed);
            else {
                userId = feed[0]["UserId"];
                if (!userId || userId == '') {
                    getContext().getResponse().setBody("[]");
                    return;
                }
                else {
                    //Query documents by UserId 
                    var isAccepted = collection.queryDocuments(
                        collection.getSelfLink(),
                        'SELECT * FROM root r WHERE r.UserId = "' + userId + '"',
                        function (err, feed, options) {
                            if (err) throw err;
                            getContext().getResponse().setBody(feed);
                        });
                }

            }
        });

}

