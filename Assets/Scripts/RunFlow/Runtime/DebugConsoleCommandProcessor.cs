using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RunFlow
{
    public readonly struct DebugConsoleCommandResult
    {
        public DebugConsoleCommandResult(bool success, string message)
        {
            Success = success;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public string Message { get; }
    }

    public class DebugConsoleCommandProcessor
    {
        private const string HelpMessage =
            "Available commands:\n" +
            "help\n" +
            "meta reset\n" +
            "money add <amount>\n" +
            "meta add <amount>\n" +
            "lives set <value>\n" +
            "lives add <delta>\n" +
            "card add <id>\n" +
            "augment add <id>";

        private readonly RunCoordinator coordinator;

        public DebugConsoleCommandProcessor(RunCoordinator coordinator)
        {
            this.coordinator = coordinator;
        }

        public DebugConsoleCommandResult Execute(string commandText)
        {
            if (coordinator == null)
                return new DebugConsoleCommandResult(false, "Debug console is unavailable.");

            string normalized = Normalize(commandText);
            if (string.IsNullOrWhiteSpace(normalized))
                return new DebugConsoleCommandResult(false, "Enter a command.");

            if (normalized == "help")
                return new DebugConsoleCommandResult(true, HelpMessage);

            if (normalized == "meta reset")
            {
                coordinator.ResetMetaProgress();
                return new DebugConsoleCommandResult(true, "Meta progress reset.");
            }

            if (normalized.StartsWith("money add", StringComparison.Ordinal))
            {
                if (!TryMatchAmountCommand(normalized, "money add", out int currencyAmount, out string amountError))
                    return new DebugConsoleCommandResult(false, amountError);

                return BuildResult(coordinator.TryGainCurrency(currencyAmount, out string message), message);
            }

            if (normalized.StartsWith("meta add", StringComparison.Ordinal))
            {
                if (!TryMatchAmountCommand(normalized, "meta add", out int metaAmount, out string amountError))
                    return new DebugConsoleCommandResult(false, amountError);

                return BuildResult(coordinator.TryGainMetaCurrency(metaAmount, out string message), message);
            }

            if (normalized.StartsWith("lives ", StringComparison.Ordinal))
            {
                if (!TryMatchLivesCommand(normalized, out bool setLives, out int livesValue, out string amountError))
                    return new DebugConsoleCommandResult(false, amountError);

                return setLives
                    ? BuildResult(coordinator.TrySetLives(livesValue, out string message), message)
                    : BuildResult(coordinator.TryAddLives(livesValue, out message), message);
            }

            if (TryMatchIdCommand(normalized, "card add", out string cardId))
                return BuildResult(coordinator.TryAddCardById(cardId, out string message), message);

            if (TryMatchIdCommand(normalized, "augment add", out string augmentId))
                return BuildResult(coordinator.TryAddAugmentById(augmentId, out string message), message);

            return new DebugConsoleCommandResult(false, $"Unknown command: {normalized}");
        }

        private static DebugConsoleCommandResult BuildResult(bool success, string message)
        {
            return new DebugConsoleCommandResult(success, message);
        }

        private static string Normalize(string commandText)
        {
            string trimmed = commandText?.Trim() ?? string.Empty;
            return Regex.Replace(trimmed.ToLowerInvariant(), "\\s+", " ");
        }

        private static bool TryMatchAmountCommand(string normalized, string prefix, out int amount, out string error)
        {
            amount = 0;
            error = null;
            if (normalized == prefix)
            {
                error = $"Expected an integer amount after '{prefix}'.";
                return false;
            }

            if (!normalized.StartsWith(prefix + " ", StringComparison.Ordinal))
                return false;

            string amountText = normalized.Substring(prefix.Length).Trim();
            if (!int.TryParse(amountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out amount))
            {
                error = $"Expected an integer amount after '{prefix}'.";
                return false;
            }

            return true;
        }

        private static bool TryMatchLivesCommand(string normalized, out bool setLives, out int value, out string error)
        {
            setLives = false;
            value = 0;
            error = null;

            if (!normalized.StartsWith("lives ", StringComparison.Ordinal))
                return false;

            string[] parts = normalized.Split(' ');
            if (parts.Length != 3 || (parts[1] != "set" && parts[1] != "add"))
            {
                error = "Use 'lives set <value>' or 'lives add <delta>'.";
                return false;
            }

            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                error = "Lives commands require an integer value.";
                return false;
            }

            setLives = parts[1] == "set";
            return true;
        }

        private static bool TryMatchIdCommand(string normalized, string prefix, out string id)
        {
            id = null;
            if (!normalized.StartsWith(prefix + " ", StringComparison.Ordinal))
                return false;

            string remainder = normalized.Substring(prefix.Length).Trim();
            if (string.IsNullOrWhiteSpace(remainder))
                return false;

            id = remainder;
            return true;
        }
    }
}
