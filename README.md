# CloudBot

## Framework

### Dependency Injection
In the dependency injection framework **no instances are explicitly created by the programmer**.   
When creating a class that is registered as a service, simply place the dependencies as constructor's parameters, they will be automatically resolved.

**NOTE: if a constructor has a concrete implementation as a dependency (class instead of interface), it means that something is architecturally wrong.**

Example
Look at the class `LogEventHandler`.   
It is possible to see that the constructor has no use references. In fact the constructor is never explicitly called.   
Instead, instances are handled with dependency injection.    
For example, in the above mentioned class, the only dependency is a `ILoggerFactory` that allows to create a logger with a certain flag.   
By simply putting a parameter of that type in the constructor, it is assured that the instance will be resolved at runtime.    
If the instance cannot be resolved, an exception is automatically thrown.   
To add a new dependency, simply add it as a parameter in the constructor.    
For example, if the `IRepository<WhitelistPreferencesModel>` would be needed (the whitelist repository handling the json file), it would be enough to change 
the constructor signature to:
```c#
public LogEventHandler(ILoggerFactory loggerFactory, IRepository<WhitelistPreferencesModel> whitelistPrefRepo)
```


### Command Modules
Command modules are automatically registered in the dependency injection framework using reflection.
To add a slash command module:
- Create a class (preferably in CommandModules folder)
- Implement the interface ISlashCommandModule
- Implement functionalities   

Done, no need to explicitly register anything, the class will be registered as a service automatically.

### Event Handlers
Event handlers are automatically registered in the dependency injection framework using reflection.
To add an event handler:
- Create a class (preferably in EventHandlers folder)
- Implement the interface IDiscordClientEventHandler
- Implement functionalities    

Done, no need to explicitly register anything, the class will be registered as a service automatically.

