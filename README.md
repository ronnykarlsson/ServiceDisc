# ServiceDisc

The purpose of ServiceDisc is to make it easier to build applications consisting of services without much infrastructure.

ServiceDisc requires common storage between clients and services. Currently [Azure Storage](https://azure.microsoft.com/en-us/services/storage/) is implemented, and in-memory storage for testing. Other storage can be implemented using the ``IServiceDiscConnection`` interface which requires methods to register/unregister services and to send/receive messages.

## Installing ServiceDisc

```powershell
Install-Package ServiceDisc
```

## Example usage

```c#
// Create a client, used for hosting a connecting to services.
// Pass an instance of AzureStorageServiceDiscConnection to store services in Azure.
var serviceDisc = new ServiceDiscClient(new InMemoryServiceDiscConnection());

// Create an instance of a class to host as a service.
var helloService = new HelloService();

// Host the service and register it in the ServiceDisc.
// The service will now be known to other clients with the same IServiceDiscConnection
await serviceDisc.HostAsync<IHelloService>(helloService);

// Create a client for communicating with the IHelloService.
// This operation will search the common storage for services implementing IHelloService
// and create a proxy class to communicate with it.
var helloServiceClient = await serviceDisc.GetAsync<IHelloService>(serviceName);

// Communicate with the service.
var world = helloServiceClient.Hello();
```
