using System;
using DotMigrate.Abstractions;

namespace DotMigrate.Builder
{
    public class MigrationOptionsBuilder<TMigrator>
    {
        private IDatabaseProvider? _provider;
        private AMigrationConfiguration? _configuration;
        private IMigrationSource? _source;

        public void WithConfiguration(AMigrationConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void WithProvider(IDatabaseProvider provider)
        {
            _provider = provider;
        }

        public void WithSource(IMigrationSource source)
        {
            _source = source;
        }

        public MigrationOptions<TMigrator> Create()
        {
            if (_configuration is null || _source is null || _provider is null)
            {
                throw new InvalidOperationException(
                    "You must provide a configuration, a migration source and a db provider before building");
            }

            return new MigrationOptions<TMigrator>
            {
                Configuration = _configuration,
                Source = _source,
                Provider = _provider
            };
        }
    }
}