# Powerplant Coding Challenge

A REST API that calculates the production plan (how much power each powerplant
should generate) given a load, fuel prices, and a list of available powerplants,
based on merit-order (cheapest-first) allocation.

## Tech stack

- .NET 8 / ASP.NET Core Web API
- C#

## How to build and run

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run
```

The API will start and listen on:

```
http://localhost:8888
```

Swagger UI is available at:

```
http://localhost:8888/swagger
```

## Docker

### Build

```bash
docker build -t powerplant-challenge .
```

### Run

```bash
docker run -p 8888:8888 powerplant-challenge
```

The API will be available at `http://localhost:8888`.

## Usage

### Endpoint

```
POST /productionplan
```

### Request body

A JSON payload containing `load`, `fuels`, and `powerplants` � see
`example_payloads/` for sample requests. This folder also contains
`response3.json`, the expected output for `payload3.json`, useful for
manually verifying the algorithm.

### Example (curl)

```bash
curl -X POST http://localhost:8888/productionplan \
  -H "Content-Type: application/json" \
  -d @example_payloads/payload3.json
```

### Response

A JSON array specifying how much power (`p`, in MW, rounded to 0.1) each
powerplant should produce. Every powerplant in the request appears in the
response, with `0.0` for plants that are not needed, e.g.:

```json
[
  { "name": "windpark1", "p": 90.0 },
  { "name": "windpark2", "p": 21.6 },
  { "name": "gasfiredbig1", "p": 460.0 },
  { "name": "gasfiredbig2", "p": 338.4 },
  { "name": "gasfiredsomewhatsmaller", "p": 0.0 },
  { "name": "tj1", "p": 0.0 }
]
```

## Approach

1. **Cost calculation**: for each powerplant, the cost per MWh is calculated
   based on its type:
   - `windturbine`: cost = 0 (no fuel), available output = `pmax * wind% / 100`
   - `gasfired`: cost = `gas price / efficiency`, plus CO2 cost
     (`0.3 ton/MWh * co2 price / efficiency`)
   - `turbojet`: cost = `kerosine price / efficiency`

   **Note on `efficiency` and wind**: `efficiency` is never used for
   `windturbine` plants — their cost is always `0` and their output only
   depends on `pmax` and `wind%`. The example payloads also set
   `efficiency: 1` for wind plants, which looks the same whether the value
   is used or ignored, so this couldn't be confirmed from the examples
   alone. It was checked directly against the original requirements
   document instead, which states that wind turbines don't consume fuel
   and are priced at zero regardless of efficiency.
2. **Merit order**: powerplants are sorted by cost per MWh, ascending
   (cheapest first).
3. **Allocation**: load is allocated to powerplants in merit order, filling
   each plant up to its maximum available output before moving to the next.
4. **Pmin handling (unit-commitment)**: if the remaining load after allocating
   cheaper plants is less than the next plant's `pmin` (but greater than
   zero), that plant is switched on at its `pmin` instead of being skipped.
   The resulting excess is then subtracted from the most expensive
   already-allocated plant(s), without dropping any plant below its own
   `pmin`. If no combination of already-allocated plants can absorb the
   excess, or if the total available capacity across all plants is
   insufficient to meet the load, the API returns a `400 Bad Request`
   explaining that no feasible production plan exists.
5. **Error handling**: input validation errors (missing powerplants, invalid
   load, infeasible load) return `400 Bad Request` with a descriptive
   message. These are logged and handled centrally via a global exception
   handler (`IExceptionHandler`, .NET 8). Unexpected errors return
   `500 Internal Server Error` the same way. An unrecognized powerplant
   `type` is rejected earlier, during JSON deserialization (the `type`
   field is a `PowerPlantType` enum bound via a custom converter), so it
   short-circuits as a standard ASP.NET Core `400` model-validation
   response before the request reaches the calculator.

## Testing

A `PowerPlantChallenge.Tests` project (xUnit) covers the core allocation logic:

- **The happy scenario**: merit-order allocation matching the known example
  (`payload3.json` / `response3.json`).
- **Pmin backtracking, both outcomes**: a plant successfully switching on
  at its `Pmin` and reducing a more expensive already-allocated plant, and
  the case where no combination of already-allocated plants can absorb the
  excess (an infeasible plan).
- **Infeasible load**: total available capacity across all plants is
  insufficient to meet the requested load.

Run the tests with:

```bash
dotnet test
```

## Bonus features

- **CO2 pricing**: emission allowance cost is factored into gas-fired plant
  pricing (`0.3 ton/MWh � co2 price / efficiency`), affecting merit order
  alongside gas cost.
- **Docker**: a `Dockerfile` is provided for containerized deployment (see
  the Docker section above).