# Kafka Consumer Client

A .NET-based Kafka consumer application that processes CloudEvents from Kafka topics and dispatches them to appropriate event handlers.

## Project Overview

This project implements a Kafka consumer that:

- Subscribes to configured Kafka topics
- Receives CloudEvents messages
- Dispatches events to appropriate handlers based on event type
- Supports multiple configuration sets for different environments

## Architecture

The application follows a modular architecture with clear separation of concerns:

- **Configuration**: Environment-specific settings for Kafka and topic configurations
- **Event Handling**: Pluggable event handlers for different event types
- **Service Layer**: Core services for event dispatching and Kafka consumption
- **Extensions**: Extension methods for service registration and configuration

## Design Patterns

The project utilizes several design patterns:

1. **Dependency Injection**: Used throughout the application for loose coupling and testability

   - Services are registered in the DI container
   - Dependencies are injected via constructor

2. **Options Pattern**: For strongly-typed configuration

   - `TopicConfigurations` and `KafkaSettings` classes
   - Configuration is bound to these classes using `IOptions<T>`

3. **Factory Pattern**: For creating Kafka consumers

   - `BuildKafkaConfig()` method in `KafkaListenerService`

4. **Strategy Pattern**: For event handling

   - `IEventHandler` interface with multiple implementations
   - Event dispatcher selects the appropriate handler based on event type

5. **Observer Pattern**: For event processing

   - Kafka consumer observes topics
   - Event dispatcher notifies appropriate handlers

6. **Extension Method Pattern**: For clean service registration
   - `ServiceCollectionExtensions` for registering services
   - `HostBuilderExtensions` for configuring the application

## Key Components

- **KafkaListenerService**: Background service that consumes messages from Kafka
- **EventDispatcher**: Routes events to appropriate handlers
- **IEventHandler**: Interface for event handlers
- **TopicConfigurations**: Configuration for topics and event handlers
- **KafkaSettings**: Kafka connection settings

## Testing

The project includes comprehensive unit tests with:

- Mock implementations for testing
- Coverage reporting using Coverlet
- HTML reports generated with ReportGenerator

## Getting Started

1. Configure Kafka settings in `appsettings.json`
2. Implement event handlers for your event types
3. Run the application using `dotnet run`

## Running Tests

Use the provided PowerShell script to run tests with coverage:

```powershell
.\tests\KafkaConsumer.Tests\run-tests.ps1
```
