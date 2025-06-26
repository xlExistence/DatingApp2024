namespace API.Data;

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using API.DataEntities;
using API.DTOs;
using API.Helpers;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

public class MessageRepository(DataContext context, IMapper mapper) : IMessageRepository
{
    public void Add(Message message) => context.Messages.Add(message);

    public void AddGroup(MessageGroup group) => context.MessageGroups.Add(group);

    public async Task<Message?> GetAsync(int id) => await context.Messages.FindAsync(id);

    public async Task<Connection?> GetConnectionAsync(string connectionId)
        => await context.Connections.FindAsync(connectionId);

    public async Task<PagedList<MessageResponse>> GetForUserAsync(MessageParams messageParams)
    {
        var query = context.Messages.OrderByDescending(m => m.MessageSent)
            .AsQueryable();

        query = messageParams.Container.ToLower(CultureInfo.InvariantCulture) switch
        {
            "inbox" => query.Where(m => m.Recipient.UserName == messageParams.Username
                && !m.RecipientDeleted),
            "outbox" => query.Where(m => m.Sender.UserName == messageParams.Username
                && !m.SenderDeleted),
            _ => query.Where(m => m.Recipient.UserName == messageParams.Username
                && m.DateRead == null
                && !m.RecipientDeleted)
        };

        var messages = query.ProjectTo<MessageResponse>(mapper.ConfigurationProvider);

        return await PagedList<MessageResponse>
            .CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
    }

    public async Task<MessageGroup?> GetMessageGroupForConnectionAsync(string connectionId)
        => await context.MessageGroups
            .Include(x => x.Connections)
            .Where(x => x.Connections.Any(c => c.ConnectionId == connectionId))
            .FirstOrDefaultAsync();

    public async Task<MessageGroup?> GetMessageGroupAsync(string groupName)
        => await context.MessageGroups
            .Include(g => g.Connections)
            .FirstOrDefaultAsync(g => g.Name == groupName);

    public async Task<IEnumerable<MessageResponse>> GetThreadAsync(string currentUsername, string recipientUsername)
    {
        var query = context.Messages
            .Where(m =>
                (m.RecipientUsername == currentUsername && !m.RecipientDeleted && m.SenderUsername == recipientUsername) ||
                (m.RecipientUsername == recipientUsername && !m.SenderDeleted && m.SenderUsername == currentUsername)
            )
            .OrderBy(m => m.MessageSent)
            .AsQueryable();

        var unreadMessages = query
            .Where(m => m.DateRead == null && m.RecipientUsername == currentUsername)
            .ToList();

        if (unreadMessages.Count != 0)
        {
            unreadMessages.ForEach(m => m.DateRead = DateTime.UtcNow);
        }

        return await query.ProjectTo<MessageResponse>(mapper.ConfigurationProvider).ToListAsync();
    }

    public void Remove(Message message) => context.Messages.Remove(message);

    public void RemoveConnection(Connection connection) => context.Connections.Remove(connection);
}

/**Add commentMore actions
Optimization result from changing INCLUDE to PROJECTTO in line 59

Include:
SELECT "m"."Id", "m"."Content", "m"."DateRead", "m"."MessageSent", "m"."RecipientDeleted", "m"."RecipientId", "m"."RecipientUsername", "m"."SenderDeleted", "m"."SenderId", "m"."SenderUsername", "a"."Id", "a"."AccessFailedCount", "a"."BirthDay", "a"."City", "a"."ConcurrencyStamp", "a"."Country", "a"."Created", "a"."Email", "a"."EmailConfirmed", "a"."Gender", "a"."Interests", "a"."Introduction", "a"."KnownAs", "a"."LastActive", "a"."LockoutEnabled", "a"."LockoutEnd", "a"."LookingFor", "a"."NormalizedEmail", "a"."NormalizedUserName", "a"."PasswordHash", "a"."PhoneNumber", "a"."PhoneNumberConfirmed", "a"."SecurityStamp", "a"."TwoFactorEnabled", "a"."UserName", "a0"."Id", "p"."Id", "p"."AppUserId", "p"."IsMain", "p"."PublicId", "p"."Url", "a0"."AccessFailedCount", "a0"."BirthDay", "a0"."City", "a0"."ConcurrencyStamp", "a0"."Country", "a0"."Created", "a0"."Email", "a0"."EmailConfirmed", "a0"."Gender", "a0"."Interests", "a0"."Introduction", "a0"."KnownAs", "a0"."LastActive", "a0"."LockoutEnabled", "a0"."LockoutEnd", "a0"."LookingFor", "a0"."NormalizedEmail", "a0"."NormalizedUserName", "a0"."PasswordHash", "a0"."PhoneNumber", "a0"."PhoneNumberConfirmed", "a0"."SecurityStamp", "a0"."TwoFactorEnabled", "a0"."UserName", "p0"."Id", "p0"."AppUserId", "p0"."IsMain", "p0"."PublicId", "p0"."Url"
      FROM "Messages" AS "m"
      INNER JOIN "AspNetUsers" AS "a" ON "m"."SenderId" = "a"."Id"
      INNER JOIN "AspNetUsers" AS "a0" ON "m"."RecipientId" = "a0"."Id"
      LEFT JOIN "Photos" AS "p" ON "a"."Id" = "p"."AppUserId"
      LEFT JOIN "Photos" AS "p0" ON "a0"."Id" = "p0"."AppUserId"
      WHERE ("m"."RecipientUsername" = @__currentUsername_0 AND NOT ("m"."RecipientDeleted") AND "m"."SenderUsername" = @__recipientUsername_1) OR ("m"."RecipientUsername" = @__recipientUsername_1 AND NOT ("m"."SenderDeleted") AND "m"."SenderUsername" = @__currentUsername_0)
      ORDER BY "m"."MessageSent", "m"."Id", "a"."Id", "a0"."Id", "p"."Id"

ProjectTo:
SELECT "m"."Id", "m"."SenderId", "m"."SenderUsername", "m"."RecipientId", "m"."RecipientUsername", "m"."Content", "m"."DateRead" IS NOT NULL, "m"."DateRead", "m"."MessageSent", (
          SELECT "p"."Url"
          FROM "Photos" AS "p"
          WHERE "a"."Id" = "p"."AppUserId" AND "p"."IsMain"
          LIMIT 1), (
          SELECT "p0"."Url"
          FROM "Photos" AS "p0"
          WHERE "a0"."Id" = "p0"."AppUserId" AND "p0"."IsMain"
          LIMIT 1)
      FROM "Messages" AS "m"
      INNER JOIN "AspNetUsers" AS "a" ON "m"."SenderId" = "a"."Id"
      INNER JOIN "AspNetUsers" AS "a0" ON "m"."RecipientId" = "a0"."Id"
      WHERE ("m"."RecipientUsername" = @__currentUsername_0 AND NOT ("m"."RecipientDeleted") AND "m"."SenderUsername" = @__recipientUsername_1) OR ("m"."RecipientUsername" = @__recipientUsername_1 AND NOT ("m"."SenderDeleted") AND "m"."SenderUsername" = @__currentUsername_0)
      ORDER BY "m"."MessageSent"

**/
