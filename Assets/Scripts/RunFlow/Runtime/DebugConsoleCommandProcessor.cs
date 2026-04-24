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

            if (normalized == "reset meta progress")
            {
                coordinator.ResetMetaProgress();
                return new DebugConsoleCommandResult(true, "Meta progress reset.");
            }

            if (normalized.StartsWith("gain currency", StringComparison.Ordinal))
            {
                if (!TryMatchAmountCommand(normalized, "gain currency", out int currencyAmount, out string amountError))
                    return new DebugConsoleCommandResult(false, amountError);

                return BuildResult(coordinator.TryGainCurrency(currencyAmount, out string message), message);
            }

            if (normalized.StartsWith("gain meta currency", StringComparison.Ordinal))
            {
                if (!TryMatchAmountCommand(normalized, "gain meta currency", out int metaAmount, out string amountError))
                    return new DebugConsoleCommandResult(false, amountError);

                return BuildResult(coordinator.TryGainMetaCurrency(metaAmount, out string message), message);
            }

            if (normalized.StartsWith("lives ", StringComparison.Ordinal) || normalized.StartsWith("modify lives ", StringComparison.Ordinal))
            {
                if (!TryMatchLivesCommand(normalized, out bool setLives, out int livesValue, out string amountError))
                    return new DebugConsoleCommandResult(false, amountError);

                return setLives
                    ? BuildResult(coordinator.TrySetLives(livesValue, out string message), message)
                    : BuildResult(coordinator.TryAddLives(livesValue, out message), message);
            }

            if (TryMatchIdCommand(normalized, "add card", out string cardId))
                return BuildResult(coordinator.TryAddCardById(cardId, out string message), message);

            if (TryMatchIdCommand(normalized, "add augment", out string augmentId))
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

            string commandText = normalized;
            if (commandText.StartsWith("modify lives ", StringComparison.Ordinal))
                commandText = commandText.Substring("modify ".Length);

            if (!commandText.StartsWith("lives ", StringComparison.Ordinal))
                return false;

            string[] parts = commandText.Split(' ');
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
            if (remainder.StartsWith("by id ", StringComparison.Ordinal))
                remainder = remainder.Substring("by id ".Length).Trim();

            if (string.IsNullOrWhiteSpace(remainder))
                return false;

            id = remainder;
            return true;
        }
    }
}
