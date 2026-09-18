using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Rbac.Commands.CreateRole;
using POS.Application.UseCases.Rbac.Commands.UpdateRole;
using POS.Application.UseCases.Rbac.Commands.UpdateRolePermissions;
using POS.Application.UseCases.Rbac.Queries.GetPermissions;
using POS.Application.UseCases.Rbac.Queries.GetResources;
using POS.Application.UseCases.Rbac.Queries.GetRoleById;
using POS.Application.UseCases.Rbac.Queries.GetRolePermissions;
using POS.Application.UseCases.Rbac.Queries.GetRoles;
using POS.Domain.Common;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
using POS.Domain.Rbac.Enums;
using POS.Domain.Rbac.Errors;
using POS.Domain.Stores;

namespace POS.Application.Tests.Roles;

public class RoleManagementTests
{
    private readonly IRoleRepository roleRepository = Substitute.For<IRoleRepository>();
    private readonly IPermissionRepository permissionRepository = Substitute.For<IPermissionRepository>();
    private readonly IStoreRepository storeRepository = Substitute.For<IStoreRepository>();
    private readonly IEmployeeStoreAccessRepository accessRepository = Substitute.For<IEmployeeStoreAccessRepository>();
    private readonly ICacheService cacheService = Substitute.For<ICacheService>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task CreateRole_Success_WhenChainOwner()
    {
        var storeId = Guid.NewGuid();
        var store = new Store("Test Store", "Address", "0123456789", "Asia/Ho_Chi_Minh", "VND", isActive: true);
        currentUser.IsChainOwner.Returns(true);
        storeRepository.GetByIdAsync(storeId, Arg.Any<CancellationToken>()).Returns(store);
        roleRepository.ExistsByNameAsync("Inventory Staff", storeId, null, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateRoleCommandHandler(roleRepository, storeRepository, accessRepository, currentUser, unitOfWork);
        var result = await handler.Handle(new CreateRoleCommand("Inventory Staff", "Manages stock", storeId), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Inventory Staff", result.Value!.Name);
        Assert.Equal("Manages stock", result.Value.Description);
        Assert.False(result.Value.IsSystemRole);
        Assert.Equal(storeId, result.Value.StoreId);

        await roleRepository.Received(1).AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRole_Success_WhenStoreManagerOfOwnStore()
    {
        var storeId = Guid.NewGuid();
        var store = new Store("Test Store", "Address", "0123456789", "Asia/Ho_Chi_Minh", "VND", isActive: true);
        currentUser.IsChainOwner.Returns(false);
        currentUser.StoreId.Returns(storeId);
        storeRepository.GetByIdAsync(storeId, Arg.Any<CancellationToken>()).Returns(store);
        roleRepository.ExistsByNameAsync("Barista Lead", storeId, null, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateRoleCommandHandler(roleRepository, storeRepository, accessRepository, currentUser, unitOfWork);
        var result = await handler.Handle(new CreateRoleCommand("Barista Lead", "Custom", storeId), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Barista Lead", result.Value!.Name);
    }

    [Fact]
    public async Task CreateRole_Fails_WhenNonChainOwnerCreatesChainWideRole()
    {
        currentUser.IsChainOwner.Returns(false);
        currentUser.StoreId.Returns(Guid.NewGuid());

        var handler = new CreateRoleCommandHandler(roleRepository, storeRepository, accessRepository, currentUser, unitOfWork);
        var result = await handler.Handle(new CreateRoleCommand("Chain Wide Custom", null, null), default);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.Forbidden.Code, result.Error.Code);
        await roleRepository.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRole_Fails_WhenStoreManagerCreatesRoleForAnotherStore()
    {
        var myStore = Guid.NewGuid();
        var otherStore = Guid.NewGuid();
        currentUser.IsChainOwner.Returns(false);
        currentUser.StoreId.Returns(myStore);
        accessRepository.ExistsAsync(Arg.Any<Guid>(), otherStore, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateRoleCommandHandler(roleRepository, storeRepository, accessRepository, currentUser, unitOfWork);
        var result = await handler.Handle(new CreateRoleCommand("Other Role", null, otherStore), default);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.Forbidden.Code, result.Error.Code);
        await roleRepository.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRole_Fails_WhenStoreNotFoundOrInactive()
    {
        var storeId = Guid.NewGuid();
        currentUser.IsChainOwner.Returns(true);
        storeRepository.GetByIdAsync(storeId, Arg.Any<CancellationToken>()).Returns((Store?)null);

        var handler = new CreateRoleCommandHandler(roleRepository, storeRepository, accessRepository, currentUser, unitOfWork);
        var result = await handler.Handle(new CreateRoleCommand("Shift Lead", null, storeId), default);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.InvalidStore.Code, result.Error.Code);
        await roleRepository.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRole_Fails_WhenRoleNameAlreadyExistsInScope()
    {
        var storeId = Guid.NewGuid();
        var store = new Store("Test Store", "Address", "0123456789", "Asia/Ho_Chi_Minh", "VND", isActive: true);
        currentUser.IsChainOwner.Returns(true);
        storeRepository.GetByIdAsync(storeId, Arg.Any<CancellationToken>()).Returns(store);
        roleRepository.ExistsByNameAsync("Cashier", storeId, null, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateRoleCommandHandler(roleRepository, storeRepository, accessRepository, currentUser, unitOfWork);
        var result = await handler.Handle(new CreateRoleCommand("Cashier", null, storeId), default);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.NameAlreadyExists.Code, result.Error.Code);
        await roleRepository.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateRole_Success_ForCustomRole()
    {
        var storeId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var role = new Role("Old Name", isSystemRole: false, storeId: storeId, description: "Old desc", id: roleId);
        currentUser.IsChainOwner.Returns(false);
        currentUser.StoreId.Returns(storeId);
        roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
        roleRepository.ExistsByNameAsync("New Name", storeId, roleId, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new UpdateRoleCommandHandler(roleRepository, accessRepository, currentUser, unitOfWork);
        var result = await handler.Handle(new UpdateRoleCommand(roleId, "New Name", "New desc"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Value!.Name);
        Assert.Equal("New desc", result.Value.Description);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateRole_Fails_WhenAccessingOtherStoreRole()
    {
        var storeA = Guid.NewGuid();
        var storeB = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var role = new Role("Store B Role", isSystemRole: false, storeId: storeB, id: roleId);
        currentUser.IsChainOwner.Returns(false);
        currentUser.StoreId.Returns(storeA);
        roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);

        var handler = new UpdateRoleCommandHandler(roleRepository, accessRepository, currentUser, unitOfWork);
        var result = await handler.Handle(new UpdateRoleCommand(roleId, "Tampered Name"), default);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.Forbidden.Code, result.Error.Code);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateRole_Fails_WhenSystemRole()
    {
        var roleId = Guid.NewGuid();
        var systemRole = new Role(RoleNames.Owner, isSystemRole: true, storeId: null, id: roleId);
        currentUser.IsChainOwner.Returns(true);
        roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(systemRole);

        var handler = new UpdateRoleCommandHandler(roleRepository, accessRepository, currentUser, unitOfWork);
        var result = await handler.Handle(new UpdateRoleCommand(roleId, "Renamed Owner", null), default);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.SystemRoleCannotBeModified.Code, result.Error.Code);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateRolePermissions_Success_And_BatchInvalidatesCacheForEmployees()
    {
        var storeId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var customRole = new Role("Custom Role", isSystemRole: false, storeId: storeId, id: roleId);
        currentUser.IsChainOwner.Returns(true);
        roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(customRole);

        var resource = new Resource("roles", "Role Management");
        var perm1Id = Guid.NewGuid();
        var perm2Id = Guid.NewGuid();
        var perm1 = new Permission(resource.Id, resource, PermissionAction.Read, "Read roles", perm1Id);
        var perm2 = new Permission(resource.Id, resource, PermissionAction.Create, "Create roles", perm2Id);

        permissionRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([perm1, perm2]);

        var emp1 = Guid.NewGuid();
        var emp2 = Guid.NewGuid();
        roleRepository.GetEmployeeIdsByRoleIdAsync(roleId, Arg.Any<CancellationToken>())
            .Returns([emp1, emp2]);

        var actorId = Guid.NewGuid();
        currentUser.EmployeeId.Returns(actorId);

        var handler = new UpdateRolePermissionsCommandHandler(
            roleRepository,
            permissionRepository,
            accessRepository,
            cacheService,
            currentUser,
            unitOfWork);

        var result = await handler.Handle(new UpdateRolePermissionsCommand(roleId, [perm1Id, perm2Id]), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Permissions.Count);

        await roleRepository.Received(1).UpdatePermissionsAsync(
            roleId,
            Arg.Is<IEnumerable<Guid>>(ids => ids.Count() == 2),
            actorId,
            Arg.Any<CancellationToken>());

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        await cacheService.Received(1).RemoveRangeAsync(
            Arg.Is<IEnumerable<string>>(keys =>
                keys.Contains($"perm:{emp1}") &&
                keys.Contains($"perm:{emp2}") &&
                keys.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateRolePermissions_Fails_WhenAccessingOtherStoreRole()
    {
        var storeA = Guid.NewGuid();
        var storeB = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var customRole = new Role("Custom Role", isSystemRole: false, storeId: storeB, id: roleId);
        currentUser.IsChainOwner.Returns(false);
        currentUser.StoreId.Returns(storeA);
        roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(customRole);

        var handler = new UpdateRolePermissionsCommandHandler(
            roleRepository,
            permissionRepository,
            accessRepository,
            cacheService,
            currentUser,
            unitOfWork);

        var result = await handler.Handle(new UpdateRolePermissionsCommand(roleId, [Guid.NewGuid()]), default);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.Forbidden.Code, result.Error.Code);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateRolePermissions_Fails_WhenSystemRole()
    {
        var roleId = Guid.NewGuid();
        var systemRole = new Role(RoleNames.Cashier, isSystemRole: true, storeId: null, id: roleId);
        currentUser.IsChainOwner.Returns(true);
        roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(systemRole);

        var handler = new UpdateRolePermissionsCommandHandler(
            roleRepository,
            permissionRepository,
            accessRepository,
            cacheService,
            currentUser,
            unitOfWork);

        var result = await handler.Handle(new UpdateRolePermissionsCommand(roleId, [Guid.NewGuid()]), default);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.SystemRoleCannotBeModified.Code, result.Error.Code);
        await cacheService.DidNotReceive().RemoveRangeAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateRolePermissions_Fails_WhenPermissionIdNotFound()
    {
        var roleId = Guid.NewGuid();
        var customRole = new Role("Custom Role", isSystemRole: false, storeId: null, id: roleId);
        currentUser.IsChainOwner.Returns(true);
        roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(customRole);

        var permId1 = Guid.NewGuid();
        var permId2 = Guid.NewGuid();

        var resource = new Resource("roles", "Roles");
        var perm1 = new Permission(resource.Id, resource, PermissionAction.Read, "Read", permId1);
        permissionRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([perm1]);

        var handler = new UpdateRolePermissionsCommandHandler(
            roleRepository,
            permissionRepository,
            accessRepository,
            cacheService,
            currentUser,
            unitOfWork);

        var result = await handler.Handle(new UpdateRolePermissionsCommand(roleId, [permId1, permId2]), default);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.InvalidPermissionIds.Code, result.Error.Code);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRolesQuery_ReturnsRoleList_ForChainOwner()
    {
        currentUser.IsChainOwner.Returns(true);
        var role1 = new Role(RoleNames.Owner, isSystemRole: true);
        var role2 = new Role("Store Staff", isSystemRole: false);
        roleRepository.GetRolesAsync(null, Arg.Any<CancellationToken>()).Returns([role1, role2]);

        var handler = new GetRolesQueryHandler(roleRepository, accessRepository, currentUser);
        var result = await handler.Handle(new GetRolesQuery(null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task GetRolesQuery_Fails_WhenStoreManagerQueriesUnauthorizedStore()
    {
        var myStore = Guid.NewGuid();
        var targetStore = Guid.NewGuid();
        currentUser.IsChainOwner.Returns(false);
        currentUser.StoreId.Returns(myStore);
        accessRepository.ExistsAsync(Arg.Any<Guid>(), targetStore, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new GetRolesQueryHandler(roleRepository, accessRepository, currentUser);
        var result = await handler.Handle(new GetRolesQuery(targetStore), default);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.Forbidden.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetRoleByIdQuery_Fails_WhenAccessingOtherStoreRole()
    {
        var storeA = Guid.NewGuid();
        var storeB = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var role = new Role("Other Role", isSystemRole: false, storeId: storeB, id: roleId);
        currentUser.IsChainOwner.Returns(false);
        currentUser.StoreId.Returns(storeA);
        roleRepository.GetByIdWithPermissionsAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);

        var handler = new GetRoleByIdQueryHandler(roleRepository, accessRepository, currentUser);
        var result = await handler.Handle(new GetRoleByIdQuery(roleId), default);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.Forbidden.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetRolePermissionsQuery_Fails_WhenAccessingOtherStoreRole()
    {
        var storeA = Guid.NewGuid();
        var storeB = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var role = new Role("Other Role", isSystemRole: false, storeId: storeB, id: roleId);
        currentUser.IsChainOwner.Returns(false);
        currentUser.StoreId.Returns(storeA);
        roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);

        var handler = new GetRolePermissionsQueryHandler(roleRepository, permissionRepository, accessRepository, currentUser);
        var result = await handler.Handle(new GetRolePermissionsQuery(roleId), default);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.Forbidden.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetResourcesQuery_ReturnsResourceListWithPermissions()
    {
        var resource = new Resource("orders", "Order Management");
        var perm = new Permission(resource.Id, resource, PermissionAction.Read, "Read orders");
        typeof(Resource).GetProperty(nameof(Resource.Permissions))!.SetValue(resource, new List<Permission> { perm });

        permissionRepository.GetResourcesWithPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns([resource]);

        var handler = new GetResourcesQueryHandler(permissionRepository);
        var result = await handler.Handle(new GetResourcesQuery(), default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("orders", result.Value[0].Code);
        Assert.Single(result.Value[0].Permissions);
    }
}
