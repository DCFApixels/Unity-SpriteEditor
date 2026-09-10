# Revalidate theme assets after deployment while retaining normal browser caching.
Jekyll::Hooks.register [:pages, :documents], :post_render do |page|
  revision = ENV["GITHUB_SHA"]
  next unless revision&.match?(/\A[0-9a-f]{40}\z/)
  next unless page.output_ext == ".html"

  page.output = page.output.gsub(%r{((?:src|href)="[^"?]*?/assets/[^"?]+\.(?:js|css))"}) do
    "#{Regexp.last_match(1)}?v=#{revision}\""
  end
end
