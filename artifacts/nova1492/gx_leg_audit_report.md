# Nova1492 Leg GX Audit Report

> generated: 2026-05-01 00:32:19

- converter version: `gx-pipeline-v19`
- audited rows: 49
- high risk: 4
- review risk: 21
- pass: 24
- manifest: `artifacts/nova1492/gx_leg_audit_manifest.csv`
- hierarchy: `artifacts/nova1492/gx_leg_audit_hierarchy.csv`

## High Risk

| part | name | verdict | flags | counts | dropped | bounds |
|---|---|---|---|---:|---:|---|
| `nova_mob_legs23_tk` | 탱커 | high | `large_dropped_block;catalog_count_mismatch;repair_applied;dropped_block_present` | 843/677 | 5 | `-1.356659;-0.02736;-1.721731|1.356659;1.869304;1.930888` |
| `nova_mob_legs49_otrs` | 옵테릭스 | high | `large_dropped_block;catalog_count_mismatch;dropped_block_present;center_x_outlier` | 429/500 | 5 | `-0.342192;-1.329139;-1.808486|1.37663;0.658344;0.804865` |
| `nova_mob_n_legs40_krz` | 크루저N | high | `catalog_count_mismatch` | 270/324 | 0 | `-1.107862;0.137563;-2.184672|1.093321;2.061103;2.743541` |
| `nova_mob_s_legs30_tk` | 탱커S | high | `large_dropped_block;catalog_count_mismatch;repair_applied;dropped_block_present` | 843/677 | 5 | `-1.356659;-0.02736;-1.721731|1.356659;1.869304;1.930888` |

## Review Risk

| part | name | verdict | flags | counts | dropped | bounds |
|---|---|---|---|---:|---:|---|
| `nova_mob_g_legs37_sts` | 스타쉽G | review | `dropped_block_present` | 441/366 | 0 | `-0.705175;0.003068;-1.976316|0.690918;1.838822;0.59142` |
| `nova_mob_g_legs57_ppo` | 피파울G | review | `repair_applied;center_y_outlier` | 959/546 | 0 | `-0.647355;0.87289;-1.607105|0.99857;3.008149;1.410269` |
| `nova_mob_g_legs58_pps` | 포퍼스G | review | `repair_applied;dropped_block_present` | 540/327 | 0 | `-0.89217;0.012997;-1.436019|0.899161;1.748133;1.494102` |
| `nova_mob_legs12_kb` | 코벳 | review | `dropped_block_present` | 200/211 | 0 | `-0.987994;-0.618605;-1.212834|0.988023;0.589403;1.033043` |
| `nova_mob_legs13_krz` | 크루저 | review | `dropped_block_present` | 246/300 | 0 | `-1.100605;0.158487;-1.433661|1.100608;1.941283;2.805035` |
| `nova_mob_legs15_krr` | 크롤러 | review | `repair_applied` | 280/234 | 0 | `-0.910493;0.014123;-0.750986|0.910493;1.377139;1.515178` |
| `nova_mob_legs20_spod` | 스파이더 | review | `dropped_block_present` | 642/672 | 0 | `-1.009502;-0.811496;-1.001757|1.055544;1.872332;0.840235` |
| `nova_mob_legs24_sts` | 스타쉽 | review | `dropped_block_present` | 441/366 | 0 | `-0.705175;0.003068;-1.976316|0.690918;1.838822;0.59142` |
| `nova_mob_legs25_kd` | 쿼더 | review | `dropped_block_present` | 476/370 | 0 | `-0.746558;-0.03408;-1.328704|0.760806;0.926688;1.723791` |
| `nova_mob_legs34_dpns` | 델피누스 | review | `repair_applied` | 325/195 | 0 | `-1.177795;-0.304557;-1.477319|1.125382;1.194538;1.335887` |
| `nova_mob_legs3_ktpr` | 캐터필러 | review | `repair_applied` | 204/222 | 0 | `-0.599133;-0.136053;-1.28457|0.599636;1.525346;1.050141` |
| `nova_mob_legs50_pps` | 포퍼스 | review | `repair_applied;dropped_block_present` | 540/327 | 0 | `-0.89217;0.012997;-1.436019|0.899161;1.748133;1.494102` |
| `nova_mob_legs51_ppo` | 피파울 | review | `repair_applied;center_y_outlier` | 959/546 | 0 | `-0.647355;0.87289;-1.607105|0.99857;3.008149;1.410269` |
| `nova_mob_legs52_pkk` | 피코크 | review | `repair_applied` | 572/620 | 0 | `-0.63399;-0.001996;-1.408099|0.627338;1.716262;1.518381` |
| `nova_mob_legs53_rgd` | 래거드 | review | `repair_applied` | 868/724 | 0 | `-1.232452;-0.002329;-1.266422|1.232452;1.024101;1.04594` |
| `nova_mob_legs9_ptr` | 패트롤 | review | `dropped_block_present` | 98/128 | 0 | `-1.071926;-0.079448;-1.384917|1.075966;0.446548;0.788307` |
| `nova_mob_n_legs42_krr` | 크롤러N | review | `repair_applied` | 280/234 | 0 | `-0.911218;0.013809;-0.750986|0.913458;1.37857;1.518738` |
| `nova_mob_n_legs44_hord` | 하이로더N | review | `repair_applied` | 378/378 | 0 | `-0.765812;-0.045486;-1.144133|0.571137;1.498746;0.992366` |
| `nova_mob_s_legs28_krz` | 크루저S | review | `dropped_block_present` | 246/300 | 0 | `-1.100605;0.158487;-1.433661|1.100608;1.941283;2.805035` |
| `nova_mob_s_legs32_kb` | 코벳S | review | `dropped_block_present` | 200/211 | 0 | `-0.987994;-0.618605;-1.212834|0.988023;0.589403;1.033043` |
| `nova_mob_s_legs33_spod` | 스파이더S | review | `dropped_block_present` | 642/672 | 0 | `-1.009502;-0.811496;-1.001757|1.055544;1.872332;0.840235` |

## Notes

- `high` means a mechanical issue must be investigated before visual acceptance.
- `review` means the row is mechanically convertible but should be included in manual capture review.
- Visual acceptance remains blocked until the capture sheet is manually compared.
