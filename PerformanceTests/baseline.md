# BugTracker – Performance Baseline

## 1. Test Environment

- Framework: ASP.NET Core .NET 8
- Database: SQL Server
- Environment: Development (Local)
- Testing Tool: Grafana k6
- Test Type: Read Operations
- Test Script: `baseline-reads.js`
- Test Duration: 30 seconds per scenario
- Rate Limiting: Enabled

## 2. Global Performance Results

| Metric               |      1 VU |    10 VUs |    50 VUs |
| -------------------- | --------: | --------: | --------: |
| Total Requests       |       136 |       840 |     1,072 |
| Throughput (req/s)   |      4.46 |     26.00 |     26.87 |
| Average              |  98.67 ms | 246.09 ms |    1.54 s |
| Median               |  25.09 ms |  66.38 ms | 844.72 ms |
| P90                  | 487.37 ms |    1.17 s |    4.68 s |
| P95                  | 523.69 ms |    1.44 s |    5.93 s |
| Maximum              |    1.22 s |    2.57 s |    7.95 s |
| HTTP Failures        |        0% |        0% |        0% |
| Checks Passed        |      100% |      100% |      100% |
| P95 Threshold (< 3s) |      PASS |      PASS |      FAIL |

## 3. Endpoint Performance – 1 VU

| Endpoint       |   Average |       P95 |
| -------------- | --------: | --------: |
| Login          | 584.98 ms | 964.93 ms |
| Projects       |  25.38 ms |  42.67 ms |
| Project Issues |  40.00 ms |  49.03 ms |
| Members        |  37.99 ms |  72.85 ms |
| Sprints        |  20.58 ms |  49.38 ms |
| Issue Detail   |  23.60 ms |  51.38 ms |
| Comments       |  26.56 ms |  48.67 ms |
| Users          |  30.25 ms |  55.21 ms |

## 4. Endpoint Performance – 10 VUs

| Endpoint       |   Average |       P95 |
| -------------- | --------: | --------: |
| Login          |    1.42 s |    1.93 s |
| Projects       |  83.28 ms | 223.59 ms |
| Project Issues | 124.33 ms | 243.30 ms |
| Members        |  83.76 ms | 215.32 ms |
| Sprints        |  50.13 ms | 120.36 ms |
| Issue Detail   |  47.35 ms | 119.11 ms |
| Comments       |  69.54 ms | 132.73 ms |
| Users          |  85.74 ms | 173.30 ms |

## 5. Endpoint Performance – 50 VUs

| Endpoint       |   Average |    P95 |
| -------------- | --------: | -----: |
| Login          |    5.51 s | 7.13 s |
| Projects       |    1.58 s | 4.47 s |
| Project Issues |    1.08 s | 1.77 s |
| Members        | 942.53 ms | 2.07 s |
| Sprints        | 745.37 ms | 1.31 s |
| Issue Detail   | 712.64 ms | 1.17 s |
| Comments       | 864.38 ms | 1.39 s |
| Users          | 914.15 ms | 2.29 s |

## 6. Performance Analysis

### 1 VU – Light Load

- All 136 requests completed successfully.
- Response times were generally low.
- Login was the most expensive operation.

### 10 VUs – Moderate Load

- All 840 requests completed successfully.
- P95 remained below the 3-second threshold.
- Response times increased under concurrent requests.

### 50 VUs – High Load

- All 1,072 requests completed successfully.
- No HTTP failures were recorded.
- P95 reached 5.93 seconds, exceeding the 3-second threshold.
- Throughput increased only slightly compared with 10 VUs.
- Significant response-time degradation appeared under higher concurrency.

## 7. Conclusion

The initial load tests show that BugTracker handles light and moderate concurrent workloads successfully in the local Development environment.

At 50 VUs, the API continues to process requests without HTTP failures, but response times increase significantly and the P95 performance threshold is exceeded.

Further investigation and optimization are required, particularly for authentication and project-related queries.

These measurements represent the initial baseline before performance optimizations and are not production performance guarantees.
