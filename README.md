# What is Trinet Client
Trinet Client is the user interface for the Trinet smart home system. Trinet is a personal project, built to simplify controlling all of my smart home devices. This software is public purely for educational purposes and is not intended as a distribution of functional, commercial software.
It is currently an ongoing WIP and is in need of refactor for scalability. 

## Dependencies
Trinet client requires an instance of trinet core to be reachable via a network. 


## Connecting to Core
If you've deployed Core, you'll hopefully have a default administrator account created as part of the dbContext model creation. Simply login via the app to start building the rooms & adding devices


## Offline mode (WIP)
Offline mode is available for times where core in unavailable.  Adding and editing rooms and devices is not supported in offline mode, so you'll need to have connected to core at least once to sync the existing location data. Offline mode is incomplete and will likely produce
errors when performing certain actions.  Some module/device combinations may not support offline mode. 


## existing modules
- Wiz
- Nanoleaf (WIP)

### Planned future modules
- Ring doorbell
- Yale smart door lock
- NEST thermostat
