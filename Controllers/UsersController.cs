    // GET: /Users
    // Показва всички потребители и техните организации
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Извличаме потребителите от Identity таблицата
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email ?? string.Empty,
                OrganizationId = u.OrganizationId,

                // EF Core извлича името на свързаната организация
                OrganizationName = u.Organization != null
                    ? u.Organization.Name
                    : null
            })
            .ToListAsync();

        return View(users);
    }

    // GET: /Users/Create?organizationId=1
    [HttpGet]
    public async Task<IActionResult> Create(int? organizationId)
    {
        // Създаваме празен ViewModel
        var model = new CreateClientUserViewModel();

        // Ако е подадена организация, проверяваме я
        if (organizationId.HasValue)
        {
            bool isValid = await _context.Organizations
                .AnyAsync(o => o.Id == organizationId.Value && o.IsActive);

            if (!isValid)
                return NotFound();

            // Предварително избираме организацията
            model.OrganizationId = organizationId.Value;
        }

        // Зареждаме всички активни организации
        await LoadOrganizationsAsync();

        return View(model);
    }

    // Зареждаме активните организации
    private async Task LoadOrganizationsAsync()