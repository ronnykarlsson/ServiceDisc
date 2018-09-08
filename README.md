# ServiceDisc

ServiceDisc is a library for hosting and communicating between services. Service discovery is done through a common data collection. Each service will register in the collection when it's hosted to make it a simple process to reach it.

ServiceDisc requires common storage between clients and services. Currently [Azure Storage](https://azure.microsoft.com/en-us/services/storage/) is implemented, and in-memory storage for testing. Other storage can be implemented using the ``IServiceDiscConnection`` interface which requires methods to register/unregister services and to send/receive queue messages.

There are two methods of communicating between services. Web API which will create a Web API controller to host the services, and a queue service which will listen to other services and reply through message queues. Queue service is default, this can be updated through the ``ServiceDiscClient.ServiceHostFactory`` property (setting it to ``WebApiServiceHostFactory`` will host the services with Web API instead).

## Installing from [NuGet](https://www.nuget.org/packages/ServiceDisc/)

```powershell
Install-Package ServiceDisc
```

## Example

```c#
// Create a client, used for hosting and connecting to services.
// Pass an instance of AzureStorageServiceDiscConnection to store services in Azure,
// to enable having clients and services on different computers.
var serviceDisc = new ServiceDiscClient();

// Create an instance of a class to host as a service.
var helloService = new HelloService();

// Host the service and register it in the ServiceDisc.
// The service will now be known to other clients with the same IServiceDiscConnection
await serviceDisc.HostAsync<IHelloService>(helloService);

// Create a client for communicating with the IHelloService.
// This operation will search the common storage for services implementing IHelloService
// and create a proxy class to communicate with it.
var helloServiceClient = await serviceDisc.GetAsync<IHelloService>(serviceName);

// Send and receive a message from the service.
var world = helloServiceClient.Hello();
```
