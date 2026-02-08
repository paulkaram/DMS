import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/pages/LoginPage.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/',
      name: 'dashboard',
      component: () => import('@/pages/DashboardPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/explorer',
      name: 'explorer',
      component: () => import('@/pages/ExplorerPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/documents/:id',
      name: 'document-details',
      component: () => import('@/pages/DocumentDetailsPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/search',
      name: 'search',
      component: () => import('@/pages/SearchPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/activity',
      name: 'activity',
      component: () => import('@/pages/ActivityPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/scan',
      name: 'scan',
      component: () => import('@/pages/ScanPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/users',
      name: 'users',
      component: () => import('@/pages/UsersPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/delegations',
      name: 'delegations',
      component: () => import('@/pages/DelegationsPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('@/pages/SettingsPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin',
      name: 'admin',
      component: () => import('@/pages/AdminPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/favorites',
      name: 'favorites',
      component: () => import('@/pages/FavoritesPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/my-files',
      name: 'my-files',
      component: () => import('@/pages/MyFilesPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/shared-with-me',
      name: 'shared-with-me',
      component: () => import('@/pages/SharedWithMePage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/my-shared-items',
      name: 'my-shared-items',
      component: () => import('@/pages/MySharedItemsPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/recycle-bin',
      name: 'recycle-bin',
      component: () => import('@/pages/RecycleBinPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/vacation',
      name: 'vacation',
      component: () => import('@/pages/VacationPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/approvals',
      name: 'approvals',
      component: () => import('@/pages/ApprovalsPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/reports',
      name: 'reports',
      component: () => import('@/pages/ReportsPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/settings/content-types',
      name: 'content-types',
      component: () => import('@/pages/ContentTypesPage.vue'),
      meta: { requiresAuth: true }
    },
    // Administration Pages
    {
      path: '/admin/cases',
      name: 'admin-cases',
      component: () => import('@/pages/admin/CasesPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/search-config',
      name: 'admin-search-config',
      component: () => import('@/pages/admin/SearchConfigPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/scan-config',
      name: 'admin-scan-config',
      component: () => import('@/pages/admin/ScanConfigPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/export',
      name: 'admin-export',
      component: () => import('@/pages/admin/ExportConfigPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/bookmarks',
      name: 'admin-bookmarks',
      component: () => import('@/pages/admin/BookmarksPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/naming-convention',
      name: 'admin-naming-convention',
      component: () => import('@/pages/admin/NamingConventionPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/patterns',
      name: 'admin-patterns',
      component: () => import('@/pages/admin/PatternsPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/filing-plans',
      name: 'admin-filing-plans',
      component: () => import('@/pages/admin/FilingPlansPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/purpose',
      name: 'admin-purpose',
      component: () => import('@/pages/admin/PurposePage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/lookups',
      name: 'admin-lookups',
      component: () => import('@/pages/admin/LookupsPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/endpoints',
      name: 'admin-endpoints',
      component: () => import('@/pages/admin/EndpointsPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/permission-levels',
      name: 'admin-permission-levels',
      component: () => import('@/pages/admin/PermissionLevelsPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/folder-content-types',
      name: 'admin-folder-content-types',
      component: () => import('@/pages/admin/FolderContentTypePage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/content-type-builder',
      name: 'admin-content-type-list',
      component: () => import('@/pages/admin/ContentTypeListPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/content-type-builder/:id',
      name: 'admin-content-type-builder',
      component: () => import('@/pages/admin/ContentTypeBuilderPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/retention-policies',
      name: 'admin-retention-policies',
      component: () => import('@/pages/admin/RetentionPoliciesPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/users',
      name: 'admin-users',
      component: () => import('@/pages/admin/UsersPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/roles',
      name: 'admin-roles',
      component: () => import('@/pages/admin/RolesPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/structures',
      name: 'admin-structures',
      component: () => import('@/pages/admin/StructuresPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/folder-templates',
      name: 'admin-folder-templates',
      component: () => import('@/pages/admin/FolderTemplatesPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/role-permissions',
      name: 'admin-role-permissions',
      component: () => import('@/pages/admin/RolePermissionsPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/workflow-designer',
      name: 'admin-workflow-designer',
      component: () => import('@/pages/admin/WorkflowDesignerPage.vue'),
      meta: { requiresAuth: true }
    }
  ]
})

router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next({ name: 'login' })
  } else if (to.name === 'login' && authStore.isAuthenticated) {
    next({ name: 'dashboard' })
  } else {
    next()
  }
})

export default router
