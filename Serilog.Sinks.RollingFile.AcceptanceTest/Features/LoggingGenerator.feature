@AcceptanceTest
Feature: LoggingGenerator

Scenario: Logging 10 events
	Given I have entered 10 into the logger	
	When Logger log all events
	Then Should log 1 events file in log folder


Scenario: Logging 999 events
	Given I have entered 999 into the logger	
	When Logger log all events
	Then Should log 5 events file in log folder
