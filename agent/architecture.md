# /agent/architecture.md

## Architecture

The project follows a **Feature-first Clean Architecture**.

Structure:

Assets/Scripts_/Features/<FeatureName>/
Domain/
Application/
Presentation/
Infrastructure/
<FeatureName>Setup.cs      # Bootstrap: composition root
<FeatureName>Bootstrap.cs  # Bootstrap: scene-level wiring

Assets/Scripts_/Shared/

Each feature must be self-contained.

Rules:

* Do not move feature-specific code into Shared.
* Shared must only contain reusable cross-feature utilities.
* Infrastructure implements Application ports.
* Domain must stay framework-independent.
* Bootstrap (Setup/Bootstrap classes at feature root) handles composition and wiring between layers.

Features should grow independently.
Only split features when a concept gains an independent lifecycle.
Never move feature-specific code into Shared.
