Elasticsearch-Azure-PAAS
========================

This is a Visual Studio project for creating an [Elasticsearch](http://http://www.elasticsearch.org) cluster on Microsoft Azure using worker roles. 
![System Design](https://garvincasimir.files.wordpress.com/2014/10/elasticsearch-paas.png "Project Conceptual Design")

Who is this for?
---------------------------
This is for people who are interested in the idea of running elasticsearch in Azure but don't want to deal with the complexities of creating and managing virtual machines. This is also an opportunity to test Elasticsearch in a simulated distributed environment. You can run it, create a couple indexes, add more instances, mess with the Elasticsearch configuration etc. 


Do I need an Azure Account to try this?
---------------------------------------
No, it runs in the full Azure Emulator on your desktop. The project is designed to work with azure files service for data and snapshots but falls back to a resource folder in the Azure Emulator. Other than that, there is no significant difference between running this project on the Azure Emulator and publishing it to Azure. 

Why no startup tasks?
----------------------
In the original proof of concept, the java and elasticsearch installers were included in the project and therefore the logical choice for installing them was startup tasks. In this solution, there are no startup tasks because I am leaning in the direction of downloading all required artifacts after the role has started. Here are my reasons for this change in thinking: 

1. This will allow very controlled updates and changes without re-uploading the cloud project. 
2. Converting the initialization logic to managed code also has the added benefit of stepping through it with a debugger. We can now fully capitalize on the remote debugging capabilites of the Azure framework.
3. The code can now be fully covered with automated tests.  
4. Long running startup tasks such as installers can cause a role to appear unresponsive.
5. I don't know of any way to take advantave of the async capabilities of .net in startup scripts to allow tasks which are not depenedent on each other to run concurrently while waiting to run tasks that are dependent on them.
6. Doing everything in managed code allows for a lot more control and provides opportunities for customization and extensibility.
7. Downloading binaries will not take very long if they are located in a storage account so I am not too concerned about that anymore.
8. With the exception of Nuget.exe I try to avoid including binaries in open source repos
9. When the project is not attached to a specific binary no changes to the solution are required to update/downgrade versions (within a certain range of releases)
 * This makes it super easy to text the performance and stability of new updates and patches 


Running the project
-------------------
I tried my best to allow someone to clone this project and run it without doing any configuration. Unfortunately, there are a couple config steps before you can run it either in the Azure Emulator or in an Azure Cloud Service. The many configuration values are needed for the role to download and install java, then download, configure and run Elasticsearch. The default download location for Elasticsearch is their main download link so you don't need to configure that if you are just testing. For Java on the other hand, I could not find a link that didn't require me to visit a page and accept a license. This is also the case with the OpenJDK distribution. So the quickest way to get this project going is to upload the java jdk installer (jdk-8u25-windows-x64.exe) to the development storage account into a conainter called "installers" and run the cloud project.

1. Clone the project
2. Restore the Nuget packages
2. Download the java jdk installer and copy it to a url accessible to the role or copy it to the storage account associated with the role. 
  2. If you copied the installer to a storage account 
    3. Copy the full storage url to the role setting "JavaDownloadURL" (default:http://127.0.0.1:10000/devstoreaccount1/installers/jdk-8u25-windows-x64.exe)
    4. Change the role setting "JavaDownloadType" to "storage" (default:storage)
    5. The role will look for the file in the storage account created from the connection string "StorageConnection" (default: Develoment Storage)
  5. If you copied the installer to a regular website or a storage url that can be accessed publicly
    6. Copy the full url to the role setting "JavaDownloadURL"
    7. Change the role setting "JavaDownloadType" to "web" (default:storage)
8. Set the role setting "JavaInstallerName" to the name of the exe you downloaded. (default: jdk-8u25-windows-x64.exe)
9. You have the option of leaving the defaults for Elasticsearch and it will download the zip file from the Elasticsearch website. If not, you can download the elasticsearch zipfile and set the corresponding config settings just like you did for java
10. Once the project knows the names of the installers/packages it will be using and their download locations you can run the project using the full emulator.
11. You should see two elasticsearch console windows open ~~with logging information~~. 
12. To check if discovery is working, open the following url in your browser or fiddler: http://localhost:9200/_nodes
  13. When Elasticsearch has properly initialized, it should show two nodes.  

![Project Running](https://garvincasimir.files.wordpress.com/2014/11/elasticsearch-azure-paas-running1.png "Running in Emulator with Fiddler for test")

The Discovery Plugin
-----------------------
The discovery plugin gets information about the role instances from the webrole using named pipes. I would have used the java azure sdk Runtime api but the named pipe it depends on is only available when using the ProgramEntryPoint option. The code can be viewed in the [Elasticsearch-Azure-PAAS-Plugin](https://github.com/garvincasimir/Elasticsearch-Azure-PAAS-Plugin) repository. There is lots of cleaning to be done. 

Other Useful Plugins
-----------------------
Although I want to keep this project as generic as possible, if anyone has suggestions for plugins that everyone should definitely be using let me know.
