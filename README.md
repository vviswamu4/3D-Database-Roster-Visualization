# 3D-Database-Roster-Visualization

This project is used to visually display entities and relationship in a graph database as 3D objects placed in space. Neo4j is a NOSQL graph database which stores entities as nodes and relationships as edges between nodes. The database records are accessible through web APIs. The main goal of this application is to develop a 3D visualization of the database and render its contents as Game Objects.
3D visualization is created using Unity Development engine and the application is viewed using a Microsoft hololens. GameObjects are dynamically instantiated for records obtained from Neo4j database through Web API calls. The Unity scenes are compiled and built in visual studio and deployed in hololens.

## Implementation

Web API calls to database are made using coroutines in C#. Dynamic rendering of objects in scene is enabled by adding c# scripts to game object. Following are the c# scripts used.

* GameController makes web API calls to databse and dynamically renders GameObjects
* SimpleJSON is used to parse records returned from Neo4j into JSON objects
* SpatialMapping script is used to place 3D holograms in a definite space in the world.

There are other configuration and dll files required to build the application.

## Installation and Deployment

To install the application, the following softwares are required.

* Unity for Hololens with Virtual Reality Supported
* Windows 10 supporting Windows Holographis Development
* Visual Studio 2015

The application is built in Unity as a Virtual Reality Application supported on Windows Platform. It is them compiled and built in Visual Studio. Finally, it is deployed on the hololens through Wifi or USB cable.

## API References

https://developer.microsoft.com/en-us/windows/holographic/documentation

