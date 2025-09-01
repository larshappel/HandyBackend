# Gemini Explanation for "Toriyasu Backend"

Here we have the Dotnet (.NET) project using ASP.NET for a backend to
Toriyasu's "Handy" Android Barcode Scanner app.

This app's purpose is to provide an endpoint to the frontend app (Android) to
send the scanned data to. The data has the scanned product ID, an amount and an
optional individual identification number to, for example, record what exact
type of meat was used for a product.

Locally in appsettings.json (not in the Git repo) there should be some
connection strings for the DB.
In the production environment the database will be on premises at Toriyasu's office.
For development we're using either a local DB, or one on another machine in the office.
