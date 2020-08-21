# Introduction 
D2S, my attempt to get rid of annoying SSIS packages, which always seem to sit in between my flat files and my SQL tables in the ingestion layer of my database. Made to fit the technology stack (and knowledge) I had available at the time. It uses C# and can work with excel, text, and csv files.

# Description
The library is made up of several namespaces, relating to some part of an ETL process. You've got your Extractors, Transformers, and Loaders for example, as well as Utilities, helpers, and services which basically provide overhead functions. The idea is to take the delegate methods out of the ETL classes and stack them like legos to make a 'Pipeline' that loads your data. Utilities/services etc are used to manage DB connections, settings, and logging etc. Special mention goes to PipelineContext.cs which may as well be called 'GlobalStateInfo.cs' for all the things it keeps track of and how incredibly entrenched it is everywhere in the library. Also contains a few helper methods to make it even more bloated. I don't like this class, nor will you but you will be using it everywhere.

On top of the ETL classes sits the Pipelines namespace, which contains several pre-written fully functional pipelines which can handle simple jobs in only a few lines of calling code. See getting started for an example

# Getting Started
First of all, clone the project and build it locally. If you are using visual studio you should be good to go at this point. If you are using something else and that something doesn't come with a SQLExpress install you will need to look into app.config and update the connectionstring in there to point to an existing sql server instance/db on your system. After this the tests should all run (there is a slight concurrency issue with one of the excel reading tests, just rerun that one if its the only one that fails)

# Your first pipeline
For a quick and simple ETL process for a csv / text file you can use the Pipeline abstract class. For a simple example check out the StartAsyncTest method in the ScalingParralelPipelineTests.cs file. In short, the following steps are taken:

1. Create a pipelinecontext object and set at least the following properties:

    a. SourceFilePath, a string containing the full path of the file you want to load
  
    b. Delimiter, the delimiter used in your file (can be anything, multicharacter delimiters are supported)
  
    c. DestinationtableName, the name of the target table in the form "schema.table"
  
    d. Qualifier, i often omit this one in the tests but if its something other than the default ("") you will want to set it. Set it to null if there is no qualifier in your data to prevent the library from encountering a stray qoutation mark and interpreting it wrong.

2. Optionally, set IsCreatingTable, IsDroppingTable, IsTruncatingtable to true/false based on needs. When creating tables, schemas are inferred from the file. IsSuggestingDatatypes will read part of the file and guess the SQL datatypes based on the content it sees. If set to false everything will be varchar
3. Call Pipeline.CreatePipeline and supply the context you've just made and a pipelineoptions enumerator. Set the enumerator to None to get the default pipeline.
4. Optionally, subscribe to the LinesReadFromFile event, which tracks rowcounts as the file is being read.
5. Having stored the output of Pipeline.CreatePipeline in a variable, call StartAsync() and store the task in a variable (or wait directly on the same line).
6. Verify your target table is filled with your precious data.
  

# Other than that...
Take a look through the various unit tests that create pipelines to get a grasp of how you can build them yourself. Theres lots of different ways to make it with various implications on resource consumption. Or ignore all that and just use the default pipeline from the abstract class. Definitely take a close look to pipelinecontext.cs tho. There is loads of settings in there that might be interesting. There is for example a bool called 'isdial' which causes the pipes to ignore the first and last line of a file! could come in handy. Oh and degreeofparralelism is a major one also if you are running this on servers. If you don't set this explicitly its just gonna default to the amount of CPU's you've got (taken from environment variable). Having 30+ cpu's all sending stuff to your server will not improve speed (i typically use 4-8, bit less if im writing SQL to a file).

# finally
there is a console that uses this library which provides a simple CLI to do the typical workload this lib is intended for. I will make this public at some point (hopefully soon) and link to it and you can just download that and use it out of the box if all you want to do is dump text files into SQL.

