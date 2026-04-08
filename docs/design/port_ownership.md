# Cross-Feature Port Ownership

이 문서는 피처 간 포트(interface/implementation) 소유권의 단일 근거(SSOT)다.
`architecture.md` 규칙: **포트 인터페이스는 소비자(consumer)의 `Application/Ports/`에 정의, 구현은 제공자(provider)의 `Infrastructure/`에 둔다.**

---

## 크로스 피처 포트 (Consumer ≠ Provider)

| Port | Consumer | Provider | Interface | Implementation |
|---|---|---|---|---|
| `ICombatNetworkCommandPort` | Combat | Player | `Combat/Application/Ports/` | `Player/Infrastructure/PlayerCombatNetworkPortAdapter.cs` |
| `ICombatTargetProvider` (Player) | Combat | Player | `Combat/Application/Ports/` | `Player/Infrastructure/PlayerCombatTargetProvider.cs` |
| `ICombatTargetProvider` (Enemy) | Combat | Enemy | `Combat/Application/Ports/` | `Enemy/Infrastructure/EnemyCombatTargetProvider.cs` |
| `ICombatTargetProvider` (Unit) | Combat | Unit | `Combat/Application/Ports/` | `Unit/Infrastructure/BattleEntityCombatTargetProvider.cs` |
| `ICombatTargetProvider` (Core) | Combat | Wave | `Combat/Application/Ports/` | `Wave/Infrastructure/PlayerPositionQueryAdapter.cs` (ObjectiveCoreCombatTargetProvider) |
| `IEntityAffiliationPort` | Combat | Player | `Combat/Application/Ports/` | `Player/Infrastructure/EntityAffiliationAdapter.cs` |
| `IPlayerPositionQuery` | Enemy | Wave | `Enemy/Application/Ports/` | `Wave/Infrastructure/HostilePositionQuery.cs` |
| `IUnitCompositionPort` | Garage | Unit | `Garage/Application/Ports/` | `Unit/Infrastructure/UnitCompositionProvider.cs` |
| `ISpeedModifierPort` | Player | Status | `Player/Application/Ports/` | `Status/Application/SpeedModifierAdapter.cs` |
| `IManaPort` | Skill | Player | `Skill/Application/Ports/` | `Player/Application/ManaAdapter.cs` |
| `IStatusQueryPort` | Skill | Status | `Skill/Application/Ports/` | `Status/Application/StatusQueryAdapter.cs` |
| `IUnitEnergyPort` | Unit | Player | `Unit/Application/Ports/` | `Player/Infrastructure/UnitEnergyAdapter.cs` |
| `IWaveSpawnPort` | Wave | Enemy | `Wave/Application/Ports/` | `Wave/Infrastructure/EnemySpawnAdapter.cs` |

---

## 셀프 피처 포트 (Consumer = Provider)

### Combat
| Port | Interface | Implementation |
|---|---|---|
| `ICombatTargetPort` | `Combat/Application/Ports/` | `Combat/Infrastructure/CombatTargetAdapter.cs` |
| `ICombatNetworkCommandPort` | `Combat/Application/Ports/` | `Player/Infrastructure/PlayerCombatNetworkPortAdapter.cs` |

### Enemy
| Port | Interface | Implementation |
|---|---|---|
| `ICoreObjectiveQuery` | `Enemy/Application/Ports/` | `Wave/CoreObjectiveBootstrap.cs` |

### Garage
| Port | Interface | Implementation |
|---|---|---|
| `IGarageNetworkPort` | `Garage/Application/Ports/` | `Garage/Infrastructure/GarageNetworkAdapter.cs` |
| `IGaragePersistencePort` | `Garage/Application/Ports/` | `Garage/Infrastructure/GarageJsonPersistence.cs` |

### Lobby
| Port | Interface | Implementation |
|---|---|---|
| `ILobbyNetworkCommandPort` | `Lobby/Application/Ports/` | `Lobby/Infrastructure/Photon/LobbyPhotonAdapter.cs` |
| `ILobbyNetworkCallbackPort` | `Lobby/Application/Ports/` | `Lobby/Infrastructure/Photon/LobbyPhotonAdapter.cs` |
| `ILobbyRepository` | `Lobby/Application/Ports/` | `Lobby/Infrastructure/Persistence/LobbyRepository.cs` |
| `ISceneLoaderPort` | `Lobby/Application/Ports/` | `Lobby/Infrastructure/SceneLoaderAdapter.cs` |

### Player
| Port | Interface | Implementation |
|---|---|---|
| `IEnergyPort` | `Player/Application/Ports/` | `Player/Application/EnergyAdapter.cs` |
| `IPlayerLookupPort` | `Player/Application/Ports/` | `Player/Infrastructure/PlayerLookupAdapter.cs` |
| `IPlayerMotorPort` | `Player/Application/Ports/` | `Player/Infrastructure/PlayerMotorAdapter.cs` |
| `IPlayerNetworkCommandPort` | `Player/Application/Ports/` | `Player/Infrastructure/PlayerNetworkAdapter.cs` |
| `IPlayerNetworkCallbackPort` | `Player/Application/Ports/` | `Player/Infrastructure/PlayerNetworkAdapter.cs` |
| `IPlayerSpecProvider` | `Player/Application/Ports/` | `Player/Infrastructure/DefaultPlayerSpecProvider.cs` |

### Projectile
| Port | Interface | Implementation |
|---|---|---|
| `IProjectilePhysicsPort` | `Projectile/Application/Ports/` | `Projectile/Infrastructure/ProjectilePhysicsAdapter.cs` |

### Skill
| Port | Interface | Implementation |
|---|---|---|
| `ISkillNetworkCommandPort` | `Skill/Application/Ports/` | `Skill/Infrastructure/SkillNetworkAdapter.cs` |
| `ISkillNetworkCallbackPort` | `Skill/Application/Ports/` | `Skill/Infrastructure/SkillNetworkAdapter.cs` |
| `ISkillUpgradeCommandPort` | `Skill/Application/Ports/` | `Skill/Application/SkillUpgradeAdapter.cs` |
| `ISkillUpgradeQueryPort` | `Skill/Application/Ports/` | `Skill/Application/SkillUpgradeAdapter.cs` |
| `ISkillIconPort` | `Skill/Presentation/` | `Skill/Presentation/SkillIconAdapter.cs` |
| `ISkillEffectPort` | `Skill/Presentation/` | `Skill/Presentation/SkillEffectAdapter.cs` |

### Status
| Port | Interface | Implementation |
|---|---|---|
| `IStatusNetworkCommandPort` | `Status/Application/Ports/` | `Status/Infrastructure/StatusNetworkAdapter.cs` |
| `IStatusNetworkCallbackPort` | `Status/Application/Ports/` | `Status/Infrastructure/StatusNetworkAdapter.cs` |

### Unit
| Port | Interface | Implementation |
|---|---|---|
| `ISummonExecutionPort` | `Unit/Application/Ports/` | `Unit/Infrastructure/SummonPhotonAdapter.cs` |

### Wave
| Port | Interface | Implementation |
|---|---|---|
| `IAlivePlayerQuery` | `Wave/Application/Ports/` | `Wave/Infrastructure/AlivePlayerQueryAdapter.cs` |
| `IDifficultySpawnScale` | `Wave/Application/Ports/` | `Wave/Infrastructure/RoomDifficultySpawnScaleProvider.cs` |
| `IWaveNetworkCommandPort` | `Wave/Application/Ports/` | `Wave/Infrastructure/WaveNetworkAdapter.cs` |
| `IWaveNetworkCallbackPort` | `Wave/Application/Ports/` | `Wave/Infrastructure/WaveNetworkAdapter.cs` |
| `IWaveSpawnPort` | `Wave/Application/Ports/` | `Wave/Infrastructure/EnemySpawnAdapter.cs` |
| `IWaveTablePort` | `Wave/Application/Ports/` | `Wave/Infrastructure/WaveTableData.cs` |

### Zone
| Port | Interface | Implementation |
|---|---|---|
| `IZoneEffectPort` | `Zone/Application/Ports/` | `Zone/ZoneEffectAdapter.cs` |
