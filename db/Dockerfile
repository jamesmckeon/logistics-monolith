# Liquibase for the changelogs in db/changelog. Integration tests, local development and the
# deploy pipeline all run this image, so every environment applies changes with the same
# Liquibase and driver versions.
#
# The official image has no Postgres JDBC driver, so it's added here with Liquibase's package
# manager. Both versions are pinned; change them together and test with update-testing-rollback.
#
# Changelogs are mounted, not copied in: mount db/changelog at /liquibase/changelog and pass
# --changelog-file=<module>/changelog.yaml. Liquibase records that relative path as part of each
# changeset's identity, so every environment must use the same mount point.
FROM liquibase/liquibase:5.0.4

RUN lpm add postgresql@42.7.13 --global
