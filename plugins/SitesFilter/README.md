SitesFilter
=========
SitesFilter is a filter plugin implementation to transform metrics about IIS/ASP.NET applications sourced from the PerformanceCounter plugin into human readable form.

###config.json
Currently there is only one category supported and one option..  "PreserveOriginal" determines if the orginal, un-modified metric is passed through or dropped.
```json
{
	"Categories" : {
		"ASP.NET Applications" : {
			"PreserveOriginal" : False	
		}
	}
}
```

###The Problem
The way information is encoded into the instance names in certain categories is not ideal:

| Category | Instance Example |
| --- | --- |
|ASP.NET Applications| \_LM\_W3SVC\_15\_ROOT |
|.NET CLR | w3wp#3 |

###The solution
Decode by cross-referencing the site and process ID's:

| Category | Instance Example |
| --- | --- |
|ASP.NET Applications| test\_dev\_natasha\_myob\_co\_nz\_ |
|.NET CLR | somethingelse\_dev\_natasha\_myob\_co\_nz\_ |
