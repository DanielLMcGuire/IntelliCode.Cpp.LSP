local lspconfig = require('lspconfig')
local configs   = require('lspconfig.configs')

if not configs.intellicode then
    configs.intellicode = {
        default_config = {
            -- Replace with your path
            cmd = { 'E:/Program Files/Microsoft Visual Studio/18/Community/Common7/IDE/Extensions/Microsoft/IntelliCode/intelliCodeLsp.exe' },
            filetypes = { 'c', 'cpp' },

            root_dir = function(fname)
                return lspconfig.util.find_git_ancestor(fname)
                    or lspconfig.util.path.dirname(fname)
            end,

            single_file_support = false,
        },
    }
end

lspconfig.intellicode.setup({
    on_attach = function(client, bufnr)
        client.server_capabilities.hoverProvider = false
        client.server_capabilities.definitionProvider = false
        client.server_capabilities.referencesProvider = false
        client.server_capabilities.renameProvider = false
        client.server_capabilities.codeActionProvider = false
        client.server_capabilities.semanticTokensProvider = nil
    end,
})