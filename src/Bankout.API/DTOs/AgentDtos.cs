namespace Bankout.API.DTOs;

public record AgentResponse(int Id, string AgentName, DateTime CreatedDate);

public record CreateAgentRequest(string AgentName);

public record UpdateAgentRequest(string AgentName);
