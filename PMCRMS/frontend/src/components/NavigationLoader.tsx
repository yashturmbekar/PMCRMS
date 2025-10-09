import React, { useEffect, useState, useRef } from "react";
import { useLocation } from "react-router-dom";
import PageLoader from "./PageLoader";

/**
 * NavigationLoader - Shows a loader during route transitions
 * Provides visual feedback when navigating between pages
 */
const NavigationLoader: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const location = useLocation();
  const [isNavigating, setIsNavigating] = useState(false);
  const previousPathRef = useRef(location.pathname);
  const isInitialMount = useRef(true);

  useEffect(() => {
    // Skip the loader on initial mount
    if (isInitialMount.current) {
      isInitialMount.current = false;
      previousPathRef.current = location.pathname;
      return;
    }

    // Check if the path has changed
    if (location.pathname !== previousPathRef.current) {
      setIsNavigating(true);
      previousPathRef.current = location.pathname;

      // Show loader for a brief moment to indicate navigation
      const timer = setTimeout(() => {
        setIsNavigating(false);
      }, 300); // Reduced to 300ms for faster transition

      return () => clearTimeout(timer);
    }
  }, [location.pathname]);

  if (isNavigating) {
    return <PageLoader message="Loading page..." />;
  }

  return <>{children}</>;
};

export default NavigationLoader;
